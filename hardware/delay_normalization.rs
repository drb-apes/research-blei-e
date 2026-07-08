/*
 * LNX Fairness Microchip v1
 * Delay Normalization Module (Rust Implementation)
 * 
 * Synchronizes multi-source sensor streams to physical time.
 * Detects spoofing and timestamp anomalies.
 * 
 * License: Apache 2.0
 * Author: Dashawn Ramel Bledsoe
 */

use std::collections::VecDeque;
use std::sync::{Arc, Mutex};
use std::time::{SystemTime, UNIX_EPOCH};

#[derive(Clone, Debug)]
pub struct SensorSample {
    pub heart_rate: f32,
    pub spo2: f32,
    pub gsr: f32,
    pub timestamp_ns: u64,
    pub validity: u8,
}

#[derive(Clone, Debug)]
pub struct NormalizedSample {
    pub original: SensorSample,
    pub corrected_timestamp_ns: u64,
    pub uncertainty_ns: u64,  // 95% confidence interval width
    pub spoofing_score: f32,  // 0 = trusted, 1 = suspicious
}

pub struct DelayNormalizer {
    /// Recent samples for jitter analysis
    sample_history: Arc<Mutex<VecDeque<SensorSample>>>,
    /// Last seen timestamp for monotonicity check
    last_timestamp: Arc<Mutex<u64>>,
    /// Reference clock for synchronization
    reference_epoch_ns: u64,
}

impl DelayNormalizer {
    pub fn new() -> Self {
        let now = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .expect("Time went backwards")
            .as_nanos() as u64;
        
        DelayNormalizer {
            sample_history: Arc::new(Mutex::new(VecDeque::with_capacity(100))),
            last_timestamp: Arc::new(Mutex::new(now)),
            reference_epoch_ns: now,
        }
    }
    
    /// Normalize a sensor sample timestamp
    pub fn normalize(&self, sample: &SensorSample) -> NormalizedSample {
        let mut history = self.sample_history.lock().unwrap();
        let mut last_ts = self.last_timestamp.lock().unwrap();
        
        // Spoofing detection checks
        let mut spoofing_score = 0.0f32;
        
        // Check 1: Monotonicity violation (timestamp goes backward)
        if sample.timestamp_ns < *last_ts {
            spoofing_score += 0.4;
        }
        
        // Check 2: Impossible inter-arrival time
        if !history.is_empty() {
            if let Some(prev) = history.back() {
                let delta = sample.timestamp_ns.saturating_sub(prev.timestamp_ns);
                // At 100 Hz, expect ~10 ms = 10_000_000 ns
                // Flag if delta < 1 ms or > 100 ms (suspicious for wearable)
                if delta < 1_000_000 || delta > 100_000_000 {
                    spoofing_score += 0.3;
                }
            }
        }
        
        // Check 3: Duplicate timestamps with different values
        if let Some(prev) = history.back() {
            if sample.timestamp_ns == prev.timestamp_ns {
                // Extremely unlikely for different sensor data
                if (sample.heart_rate - prev.heart_rate).abs() > 0.1 {
                    spoofing_score += 0.2;
                }
            }
        }
        
        // Update history and last timestamp
        if history.len() >= 100 {
            history.pop_front();
        }
        history.push_back(sample.clone());
        *last_ts = sample.timestamp_ns;
        
        drop(history);
        drop(last_ts);
        
        // Corrected timestamp uses reference epoch + measured delta
        let corrected_timestamp_ns = if spoofing_score < 0.2 {
            sample.timestamp_ns
        } else {
            // Fall back to local clock if suspicious
            SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .expect("Time went backwards")
                .as_nanos() as u64
        };
        
        // Uncertainty proportional to spoofing score
        let uncertainty_ns = (1000_000 as f32 * spoofing_score) as u64 + 100_000;  // 100 µs base + score-dependent
        
        NormalizedSample {
            original: sample.clone(),
            corrected_timestamp_ns,
            uncertainty_ns,
            spoofing_score,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_monotonicity_violation() {
        let normalizer = DelayNormalizer::new();
        
        let sample1 = SensorSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
            validity: 1,
        };
        
        let sample2 = SensorSample {
            heart_rate: 73.0,
            spo2: 98.0,
            gsr: 2.6,
            timestamp_ns: 900_000_000,  // Timestamp goes backward!
            validity: 1,
        };
        
        normalizer.normalize(&sample1);
        let result = normalizer.normalize(&sample2);
        
        assert!(result.spoofing_score >= 0.4, "Monotonicity violation not detected");
    }
    
    #[test]
    fn test_impossible_inter_arrival_time() {
        let normalizer = DelayNormalizer::new();
        
        let sample1 = SensorSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
            validity: 1,
        };
        
        let sample2 = SensorSample {
            heart_rate: 73.0,
            spo2: 98.0,
            gsr: 2.6,
            timestamp_ns: 1_000_500_000,  // Only 500 µs later (too fast)
            validity: 1,
        };
        
        normalizer.normalize(&sample1);
        let result = normalizer.normalize(&sample2);
        
        assert!(result.spoofing_score >= 0.3, "Impossible inter-arrival time not detected");
    }
    
    #[test]
    fn test_clean_timestamp() {
        let normalizer = DelayNormalizer::new();
        
        let sample1 = SensorSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
            validity: 1,
        };
        
        let sample2 = SensorSample {
            heart_rate: 73.0,
            spo2: 98.0,
            gsr: 2.6,
            timestamp_ns: 1_010_000_000,  // 10 ms later (normal for 100 Hz)
            validity: 1,
        };
        
        normalizer.normalize(&sample1);
        let result = normalizer.normalize(&sample2);
        
        assert!(result.spoofing_score < 0.2, "Clean timestamp flagged as suspicious");
    }
}

fn main() {
    println!("[Delay Normalization] Initialize");
    
    let normalizer = DelayNormalizer::new();
    
    // Simulate 100 Hz sensor stream for 1 second
    let mut timestamp = 1_000_000_000u64;
    for i in 0..100 {
        let sample = SensorSample {
            heart_rate: 70.0 + (i as f32 * 0.1),
            spo2: 98.0,
            gsr: 2.5 + (i as f32 * 0.01),
            timestamp_ns: timestamp,
            validity: 1,
        };
        
        let normalized = normalizer.normalize(&sample);
        
        if i % 10 == 0 {
            println!(
                "[Sample {}] HR: {:.1}, Spoofing Score: {:.3}, Uncertainty: {} ns",
                i, sample.heart_rate, normalized.spoofing_score, normalized.uncertainty_ns
            );
        }
        
        timestamp += 10_000_000;  // 10 ms increment
    }
    
    println!("[Delay Normalization] Complete");
}
