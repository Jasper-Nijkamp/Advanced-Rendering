#pragma once

#include <fstream>
#include <vector>


class PerformanceLogger {
public:
    PerformanceLogger(const char* fileName, bool append);
    void StartRecording(int resolution, float stepSizeFactor, int numFramesToLog);
    void StopRecording();
    void Update(double deltaTime);

    bool isRecording;
    bool isFinished;

private:
    void RecordParameters(int resolution, float stepSizeFactor);
    void RecordFrame(double frameTime);
    std::ofstream logFile;
    std::string metaData;

    double* frameTimes;
    int frameIndex;
    int numFrames;
};
