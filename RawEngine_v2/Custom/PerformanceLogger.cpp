//
// Created by jaspe on 11/05/2026.
//

#include "PerformanceLogger.h"

#include <string>

PerformanceLogger::PerformanceLogger(const char *fileName, const bool append) {
    logFile = std::ofstream(fileName, append ? std::ofstream::app : std::ofstream::out);
    frameTimes = nullptr;
    metaData = std::string();
    frameIndex = 0;
    numFrames = 0;

    isRecording = false;
    isFinished = false;


}

void PerformanceLogger::StartRecording(const int resolution, const float stepSizeFactor, const int numFramesToLog) {
    frameTimes = new double[numFramesToLog];
    char buffer[50];
    sprintf_s(buffer, "%d;%.2f;", resolution, stepSizeFactor);

    metaData = std::string(buffer);
    frameIndex = 0;
    this->numFrames = numFramesToLog;
    isRecording = true;
    isFinished = false;
}

void PerformanceLogger::RecordFrame(const double frameTime) {
    frameTimes[frameIndex] = frameTime;
    frameIndex++;

    if (frameIndex == numFrames) {
        StopRecording();
    }
}

void PerformanceLogger::StopRecording() {
    logFile << "Frame Index;Frame Time(ms)" << std::endl;
    for (int i = 0; i < numFrames; i++) {
        const double time = frameTimes[i] * 1000.0;
        char buffer[50];
        sprintf_s(buffer, "%d;%.3f", i, time);

        //Replace dots with commas for Excel
        const size_t len = strlen(buffer);
        for (size_t c = 0; c < len; c++) {
            if (buffer[c] == '.') buffer[c] = ',';
        }

        logFile << buffer << std::endl;
    }

    logFile << std::endl;
    logFile.close();
    metaData.clear();
    isFinished = true;
    isRecording = false;

    delete[] frameTimes;
}

void PerformanceLogger::RecordParameters(const int resolution, const float stepSizeFactor) {
    logFile << "Texture Resolution: " << resolution << std::endl;
    logFile << "Step Size Factor: " << stepSizeFactor << std::endl;
    logFile << " \n\n" << std::endl;
}

void PerformanceLogger::Update(const double deltaTime) {
    if (isRecording) RecordFrame(deltaTime);
}
