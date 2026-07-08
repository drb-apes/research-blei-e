/*
 * LNX Fairness Microchip v1
 * Digital Dye Pack Embedder (Rust Implementation)
 * 
 * Embeds cryptographic markers, tamper detection, and tracking codes.
 * 
 * License: Apache 2.0
 * Author: Dashawn Ramel Bledsoe
 */

use std::time::{SystemTime, UNIX_EPOCH};
use sha2::{Sha256, Digest};

#[derive(Clone, Debug)]
pub struct BiometricSample {
    pub heart_rate: f32,
    pub spo2: f32,
    pub gsr: f32,
    pub timestamp_ns: u64,
}

#[derive(Clone, Debug)]
pub struct DyePackSample {
    pub data: BiometricSample,
    pub watermark: Vec<u8>,      // 32-byte SHA256 hash
    pub tamper_marker: Vec<u8>,  // Merkle root
    pub tracking_nonce: Vec<u8>, // Unique identifier
    pub embedded_at_ns: u64,
}

pub struct DyePackEmbedder {
    /// Current merkle root for tamper detection
    merkle_root: Vec<u8>,
    /// Counter for unique nonce generation
    nonce_counter: u64,
}

impl DyePackEmbedder {
    pub fn new() -> Self {
        DyePackEmbedder {
            merkle_root: vec![0u8; 32],
            nonce_counter: 0,
        }
    }
    
    /// Embed dye pack markers into a biometric sample
    pub fn embed(&mut self, sample: &BiometricSample) -> DyePackSample {
        let embedded_at_ns = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .expect("Time went backwards")
            .as_nanos() as u64;
        
        // Generate watermark: HMAC-like signature of sample data
        let mut hasher = Sha256::new();
        hasher.update(format!("{}", sample.heart_rate).as_bytes());
        hasher.update(format!("{}", sample.spo2).as_bytes());
        hasher.update(format!("{}", sample.gsr).as_bytes());
        hasher.update(sample.timestamp_ns.to_le_bytes());
        let watermark = hasher.finalize().to_vec();
        
        // Update Merkle root for tamper detection
        let mut merkle_hasher = Sha256::new();
        merkle_hasher.update(&self.merkle_root);
        merkle_hasher.update(&watermark);
        self.merkle_root = merkle_hasher.finalize().to_vec();
        
        // Generate unique tracking nonce
        let mut nonce_data = Vec::new();
        nonce_data.extend_from_slice(&embedded_at_ns.to_le_bytes());
        nonce_data.extend_from_slice(&self.nonce_counter.to_le_bytes());
        
        let mut nonce_hasher = Sha256::new();
        nonce_hasher.update(&nonce_data);
        let tracking_nonce = nonce_hasher.finalize().to_vec();
        
        self.nonce_counter += 1;
        
        DyePackSample {
            data: sample.clone(),
            watermark,
            tamper_marker: self.merkle_root.clone(),
            tracking_nonce,
            embedded_at_ns,
        }
    }
    
    /// Verify integrity of a dye-packed sample
    pub fn verify(&self, sample: &DyePackSample) -> bool {
        let mut hasher = Sha256::new();
        hasher.update(format!("{}", sample.data.heart_rate).as_bytes());
        hasher.update(format!("{}", sample.data.spo2).as_bytes());
        hasher.update(format!("{}", sample.data.gsr).as_bytes());
        hasher.update(sample.data.timestamp_ns.to_le_bytes());
        let expected_watermark = hasher.finalize().to_vec();
        
        expected_watermark == sample.watermark
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_embed_and_verify() {
        let mut embedder = DyePackEmbedder::new();
        
        let sample = BiometricSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
        };
        
        let dye_sample = embedder.embed(&sample);
        assert!(embedder.verify(&dye_sample), "Verification failed for valid sample");
    }
    
    #[test]
    fn test_tamper_detection() {
        let mut embedder = DyePackEmbedder::new();
        
        let sample = BiometricSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
        };
        
        let mut dye_sample = embedder.embed(&sample);
        
        // Tamper with data
        dye_sample.data.heart_rate = 80.0;
        
        assert!(!embedder.verify(&dye_sample), "Tampering not detected");
    }
    
    #[test]
    fn test_unique_nonces() {
        let mut embedder = DyePackEmbedder::new();
        
        let sample1 = BiometricSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
        };
        
        let sample2 = BiometricSample {
            heart_rate: 72.0,
            spo2: 98.0,
            gsr: 2.5,
            timestamp_ns: 1_000_000_000,
        };
        
        let dye1 = embedder.embed(&sample1);
        let dye2 = embedder.embed(&sample2);
        
        assert_ne!(dye1.tracking_nonce, dye2.tracking_nonce, "Nonces should be unique");
    }
}

fn main() {
    println!("[Digital Dye Pack Embedder] Initialize");
    
    let mut embedder = DyePackEmbedder::new();
    
    // Embed 10 samples
    for i in 0..10 {
        let sample = BiometricSample {
            heart_rate: 70.0 + (i as f32),
            spo2: 98.0,
            gsr: 2.5 + (i as f32 * 0.1),
            timestamp_ns: 1_000_000_000 + (i as u64 * 10_000_000),
        };
        
        let dye_sample = embedder.embed(&sample);
        let verified = embedder.verify(&dye_sample);
        
        println!(
            "[Sample {}] HR: {:.1}, Verified: {}, Watermark (hex): {}",
            i,
            sample.heart_rate,
            verified,
            hex::encode(&dye_sample.watermark[0..8])
        );
    }
    
    println!("[Digital Dye Pack Embedder] Complete");
}
