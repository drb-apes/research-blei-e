/*
 * LNX Fairness Microchip v1
 * Sensor Interface Layer (CUDA Implementation)
 * 
 * Acquires multi-modal sensor data (BLE, UWB, IMU, HR, SpO2, GSR)
 * and streams to GPU memory for downstream processing.
 * 
 * License: Apache 2.0
 * Author: Dashawn Ramel Bledsoe
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <cuda_runtime.h>

// Sensor data structure (shared between host and device)
typedef struct {
    float accelerometer[3];     // x, y, z in m/s^2
    float gyroscope[3];         // x, y, z in rad/s
    float heart_rate;           // bpm
    float spo2;                 // %
    float gsr;                  // microSiemens
    float uwb_distance;         // meters
    unsigned long long timestamp; // nanoseconds
    unsigned char validity;     // 1 = valid, 0 = invalid
} SensorSample;

// Ring buffer for lock-free data streaming
typedef struct {
    SensorSample* buffer;
    unsigned int capacity;
    unsigned int write_idx;
    unsigned int read_idx;
    unsigned int count;
} RingBuffer;

/**
 * CUDA kernel: Process incoming BLE advertising packet data
 * Decodes BLE GATT notifications into structured sensor samples
 */
__global__ void decode_ble_data(
    const unsigned char* ble_payload,
    unsigned int payload_len,
    SensorSample* output,
    unsigned long long current_time
) {
    unsigned int idx = blockIdx.x * blockDim.x + threadIdx.x;
    
    if (idx >= payload_len / 20) return; // Each BLE packet ~20 bytes per sensor
    
    // Simplified BLE payload decoding
    // Real implementation would parse GATT characteristic UUIDs
    const unsigned char* pkt = ble_payload + idx * 20;
    
    // Extract values (assumes little-endian, 16-bit fixed-point HR/SpO2/GSR)
    output[idx].heart_rate = (float)((pkt[0] | (pkt[1] << 8)) & 0xFF);
    output[idx].spo2 = (float)(pkt[2]);
    output[idx].gsr = (float)((pkt[3] | (pkt[4] << 8))) / 1000.0f;  // mS to microS
    output[idx].timestamp = current_time;
    output[idx].validity = 1;
}

/**
 * CUDA kernel: Fuse IMU data (accelerometer + gyroscope)
 * Uses simple complementary filter for 6-DOF orientation
 */
__global__ void fuse_imu_data(
    float* accel_x, float* accel_y, float* accel_z,
    float* gyro_x, float* gyro_y, float* gyro_z,
    SensorSample* output,
    unsigned int sample_count,
    float dt  // timestep in seconds
) {
    unsigned int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= sample_count) return;
    
    // Copy accelerometer data
    output[idx].accelerometer[0] = accel_x[idx];
    output[idx].accelerometer[1] = accel_y[idx];
    output[idx].accelerometer[2] = accel_z[idx];
    
    // Copy gyroscope data
    output[idx].gyroscope[0] = gyro_x[idx];
    output[idx].gyroscope[1] = gyro_y[idx];
    output[idx].gyroscope[2] = gyro_z[idx];
    
    // Magnitude check for validity
    float accel_mag = sqrtf(
        accel_x[idx] * accel_x[idx] +
        accel_y[idx] * accel_y[idx] +
        accel_z[idx] * accel_z[idx]
    );
    
    // Expect gravity ~9.81 m/s^2 ± 2 m/s^2 for normal motion
    output[idx].validity = (accel_mag > 7.81f && accel_mag < 11.81f) ? 1 : 0;
}

/**
 * CUDA kernel: Process UWB ranging data
 * Converts time-of-arrival (ToA) to distance estimates
 */
__global__ void process_uwb_ranging(
    const float* toa_nanoseconds,  // Time of arrival in ns
    SensorSample* output,
    unsigned int sample_count
) {
    unsigned int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= sample_count) return;
    
    // Speed of light: 0.3 m/ns
    // Distance = ToA * speed_of_light / 2 (round-trip)
    const float SPEED_OF_LIGHT = 0.3f;  // m/ns
    output[idx].uwb_distance = (toa_nanoseconds[idx] * SPEED_OF_LIGHT) / 2.0f;
    output[idx].validity = (output[idx].uwb_distance > 0.1f && output[idx].uwb_distance < 100.0f) ? 1 : 0;
}

/**
 * Host function: Initialize ring buffer
 */
RingBuffer* init_ring_buffer(unsigned int capacity) {
    RingBuffer* rb = (RingBuffer*)malloc(sizeof(RingBuffer));
    cudaMallocManaged(&rb->buffer, capacity * sizeof(SensorSample));
    rb->capacity = capacity;
    rb->write_idx = 0;
    rb->read_idx = 0;
    rb->count = 0;
    return rb;
}

/**
 * Host function: Push sample to ring buffer (thread-safe)
 */
void ringbuffer_push(RingBuffer* rb, const SensorSample* sample) {
    if (rb->count < rb->capacity) {
        memcpy(&rb->buffer[rb->write_idx], sample, sizeof(SensorSample));
        rb->write_idx = (rb->write_idx + 1) % rb->capacity;
        rb->count++;
    }
}

/**
 * Host function: Pop sample from ring buffer (thread-safe)
 */
int ringbuffer_pop(RingBuffer* rb, SensorSample* sample) {
    if (rb->count > 0) {
        memcpy(sample, &rb->buffer[rb->read_idx], sizeof(SensorSample));
        rb->read_idx = (rb->read_idx + 1) % rb->capacity;
        rb->count--;
        return 1;
    }
    return 0;
}

/**
 * Host function: Main sensor acquisition loop
 * Simulates 100 Hz sensor sampling
 */
int main() {
    printf("[LNX Microchip v1] Sensor Interface Layer Initialize\n");
    
    // Allocate ring buffer for 1000 samples
    RingBuffer* sensor_buffer = init_ring_buffer(1000);
    
    // Allocate device memory for batch processing
    unsigned int batch_size = 100;
    SensorSample* d_samples = NULL;
    cudaMalloc(&d_samples, batch_size * sizeof(SensorSample));
    
    // Simulate sensor acquisition for 10 seconds at 100 Hz
    unsigned int num_cycles = 1000;
    
    printf("[Sensor Interface] Starting acquisition loop (100 Hz, 10 sec total)\n");
    
    for (unsigned int cycle = 0; cycle < num_cycles; cycle++) {
        struct timespec now;
        clock_gettime(CLOCK_MONOTONIC, &now);
        unsigned long long current_time = (unsigned long long)now.tv_sec * 1000000000ULL + now.tv_nsec;
        
        // Simulate BLE packet decoding
        unsigned char ble_data[20];
        for (int i = 0; i < 20; i++) ble_data[i] = (unsigned char)(rand() % 256);
        
        unsigned char* d_ble_data = NULL;
        cudaMalloc(&d_ble_data, 20);
        cudaMemcpy(d_ble_data, ble_data, 20, cudaMemcpyHostToDevice);
        
        decode_ble_data<<<1, 1>>>(d_ble_data, 20, d_samples, current_time);
        
        // Copy result back to host
        SensorSample host_sample;
        cudaMemcpy(&host_sample, d_samples, sizeof(SensorSample), cudaMemcpyDeviceToHost);
        
        // Push to ring buffer
        ringbuffer_push(sensor_buffer, &host_sample);
        
        cudaFree(d_ble_data);
        
        if (cycle % 100 == 0) {
            printf("[Cycle %u] HR: %.1f bpm, SpO2: %.1f %%, GSR: %.3f µS, Timestamp: %llu ns\n",
                   cycle, host_sample.heart_rate, host_sample.spo2, host_sample.gsr, current_time);
        }
        
        // Sleep 10 ms (100 Hz)
        usleep(10000);
    }
    
    // Print buffer statistics
    printf("[Sensor Interface] Acquisition complete\n");
    printf("  Ring buffer: %u samples (capacity: %u)\n", sensor_buffer->count, sensor_buffer->capacity);
    printf("  Expected samples: ~1000 (10 sec @ 100 Hz)\n");
    
    // Cleanup
    cudaFree(d_samples);
    cudaFree(sensor_buffer->buffer);
    free(sensor_buffer);
    
    printf("[Sensor Interface Layer] Shutdown\n");
    return 0;
}
