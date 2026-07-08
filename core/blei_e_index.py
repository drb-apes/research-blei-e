#!/usr/bin/env python3
"""
BLEI-E Index Computation

Multi-factor athlete performance scoring combining:
- Physiological signals (RSI, heart rate variability)
- Environmental context (altitude, circadian phase)
- Volatility and volatility clustering
- Biomechanical rhythm coherence
- Cognitive-stress markers
- Chrono-stability (temporal consistency)

License: Apache 2.0
Author: Dashawn Ramel Bledsoe
"""

import numpy as np
from typing import Dict, Tuple
from dataclasses import dataclass
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class BiometricSample:
    """Single athlete biometric measurement"""
    heart_rate: float  # bpm
    spo2: float  # %
    gsr: float  # microSiemens
    acceleration_x: float  # m/s^2
    acceleration_y: float
    acceleration_z: float
    timestamp_ns: int  # nanoseconds

@dataclass
class BLEIEFactors:
    """Component factors for BLEI-E index"""
    rsi: float  # Reactive Strength Index (0-100)
    ef: float   # Environmental Factor (0-1)
    vv: float   # Volatility-Variance Factor (0-1)
    brf: float  # Biomechanical Rhythm Factor (0-1)
    csf: float  # Cognitive-Stress Factor (0-1)
    cs: float   # Chrono-Stability Factor (0-1)

class BLEIEIndex:
    """
    BLEI-E (Biometric-Linked Equities Index - Explosive) computation engine.
    
    Transforms raw biometric data into a normalized 0-1 explosive readiness score.
    """
    
    def __init__(self):
        """Initialize factor weights (learned from training data)"""
        self.weights = {
            'rsi': 0.25,
            'ef': 0.10,
            'vv': -0.15,  # Negative: lower volatility is better
            'brf': -0.10,
            'csf': -0.15,
            'cs': 0.25,
        }
        self.baseline_hr = 60.0  # resting heart rate
        self.baseline_spo2 = 95.0
    
    def compute_rsi(self, sample: BiometricSample, prev_sample: BiometricSample = None) -> float:
        """
        Reactive Strength Index (RSI)
        Measures explosive readiness from acceleration/deceleration patterns.
        """
        # Acceleration magnitude
        accel_mag = np.sqrt(
            sample.acceleration_x**2 +
            sample.acceleration_y**2 +
            sample.acceleration_z**2
        )
        
        # Normalize to 0-100 scale (typical range: 0-25 m/s^2)
        rsi = min(100.0, (accel_mag / 25.0) * 100.0)
        return rsi
    
    def compute_environmental_factor(self, altitude_m: float = 0, circadian_phase: float = 0.5) -> float:
        """
        Environmental Factor (EF)
        Adjusts performance based on training conditions.
        """
        # Altitude adjustment (0-2500 m typical range)
        altitude_penalty = max(0, (altitude_m - 500) / 2500.0) * 0.3
        
        # Circadian adjustment (0-1 phase, 0.5 = midday peak)
        circadian_boost = np.cos(2 * np.pi * (circadian_phase - 0.5)) * 0.2
        
        ef = 1.0 - altitude_penalty + circadian_boost
        return np.clip(ef, 0.0, 1.0)
    
    def compute_volatility_variance(self, samples: np.ndarray) -> float:
        """
        Volatility-Variance Factor (VV)
        Lower variance = more consistent performance = lower factor.
        """
        if len(samples) < 2:
            return 0.0
        
        # Coefficient of variation in acceleration
        accel_mag = np.sqrt(
            samples[:, 0]**2 + samples[:, 1]**2 + samples[:, 2]**2
        )
        mean_accel = np.mean(accel_mag)
        if mean_accel < 0.1:  # Avoid division by zero
            return 0.0
        
        cv = np.std(accel_mag) / mean_accel
        vv = np.clip(cv, 0.0, 1.0)  # Higher CV = higher volatility
        return vv
    
    def compute_biomechanical_rhythm_factor(self, samples: np.ndarray) -> float:
        """
        Biomechanical Rhythm Factor (BRF)
        Measures phase-locking and coherence of movement patterns.
        """
        if len(samples) < 10:
            return 0.5
        
        # Simplified: compute spectral entropy
        # Higher entropy = less coordinated movement = higher BRF
        fft_accel = np.fft.fft(samples[:, 0])
        power_spectrum = np.abs(fft_accel)**2
        normalized_spectrum = power_spectrum / np.sum(power_spectrum)
        entropy = -np.sum(normalized_spectrum[normalized_spectrum > 0] * 
                          np.log2(normalized_spectrum[normalized_spectrum > 0]))
        
        # Normalize entropy to 0-1 range (typical: 0-8 bits)
        brf = np.clip(entropy / 8.0, 0.0, 1.0)
        return brf
    
    def compute_cognitive_stress_factor(self, sample: BiometricSample) -> float:
        """
        Cognitive-Stress Factor (CSF)
        Derived from heart rate variability, GSR, breathing rate proxies.
        """
        # Normalize heart rate to resting (50-100 bpm typical range)
        hr_norm = (sample.heart_rate - self.baseline_hr) / 40.0
        hr_stress = np.clip(hr_norm, 0.0, 1.0)
        
        # GSR as stress indicator (0.5-10 microSiemens typical)
        gsr_norm = min(1.0, sample.gsr / 5.0)
        
        # SpO2 as recovery indicator (higher = better)
        spo2_recovery = 1.0 - np.clip((100 - sample.spo2) / 10.0, 0.0, 1.0)
        
        # Composite stress
        csf = (0.5 * hr_stress + 0.3 * gsr_norm - 0.2 * spo2_recovery)
        return np.clip(csf, 0.0, 1.0)
    
    def compute_chrono_stability_factor(self, css_score: float) -> float:
        """
        Chrono-Stability Factor (CS)
        Integration of CSS ensemble output.
        """
        return css_score  # Direct pass-through from CSS module
    
    def compute(self, sample: BiometricSample, 
                prev_sample: BiometricSample = None,
                samples_window: np.ndarray = None,
                css_score: float = 0.5,
                environmental_context: Dict = None) -> Tuple[float, BLEIEFactors]:
        """
        Compute BLEI-E index (0-1 scale).
        
        Args:
            sample: Current biometric measurement
            prev_sample: Previous measurement (optional, for delta calculations)
            samples_window: Window of recent samples for variance/rhythm analysis
            css_score: Chrono-Stability Score from CSS ensemble (0-1)
            environmental_context: Dict with altitude, circadian_phase, etc.
        
        Returns:
            (blei_e_score, factors_breakdown)
        """
        env = environmental_context or {}
        
        # Compute individual factors
        rsi = self.compute_rsi(sample, prev_sample)
        ef = self.compute_environmental_factor(
            altitude_m=env.get('altitude_m', 0),
            circadian_phase=env.get('circadian_phase', 0.5)
        )
        
        if samples_window is not None and len(samples_window) > 1:
            vv = self.compute_volatility_variance(samples_window)
            brf = self.compute_biomechanical_rhythm_factor(samples_window)
        else:
            vv = 0.0
            brf = 0.0
        
        csf = self.compute_cognitive_stress_factor(sample)
        cs = self.compute_chrono_stability_factor(css_score)
        
        # Normalize factors to 0-1 range
        rsi_norm = min(1.0, rsi / 100.0)
        
        # Weighted composite
        composite = (
            self.weights['rsi'] * rsi_norm +
            self.weights['ef'] * ef +
            self.weights['vv'] * vv +
            self.weights['brf'] * brf +
            self.weights['csf'] * csf +
            self.weights['cs'] * cs
        )
        
        # Apply sigmoid for bounded output
        blei_e_score = 1.0 / (1.0 + np.exp(-composite * 2))
        
        factors = BLEIEFactors(
            rsi=rsi_norm,
            ef=ef,
            vv=vv,
            brf=brf,
            csf=csf,
            cs=cs
        )
        
        logger.info(f"BLEI-E Score: {blei_e_score:.4f} (RSI: {rsi:.1f}, EF: {ef:.3f}, CS: {cs:.3f})")
        
        return blei_e_score, factors


def main():
    """Example BLEI-E computation"""
    print("[BLEI-E Index] Initialize")
    
    blei = BLEIEIndex()
    
    # Simulate 10 samples
    for i in range(10):
        sample = BiometricSample(
            heart_rate=70.0 + i * 1.5,
            spo2=98.0,
            gsr=2.5 + i * 0.1,
            acceleration_x=9.81 + i * 0.5,
            acceleration_y=0.5,
            acceleration_z=0.2,
            timestamp_ns=1_000_000_000 + i * 10_000_000
        )
        
        # Simulate CSS score
        css_score = 0.5 + i * 0.03
        
        score, factors = blei.compute(
            sample,
            css_score=css_score,
            environmental_context={'altitude_m': 0, 'circadian_phase': 0.6}
        )
        
        print(f"[Sample {i}] BLEI-E: {score:.4f}, Factors: RSI={factors.rsi:.3f}, CS={factors.cs:.3f}")
    
    print("[BLEI-E Index] Complete")


if __name__ == "__main__":
    main()
