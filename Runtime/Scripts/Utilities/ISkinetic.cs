using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinetic
{
    public interface ISkinetic
    {
        void InitInstance();
        void DeinitInstance();


        int ScanDevices(SkineticDevice.OutputType output);
        int ScanStatus();
        List<SkineticDevice.DeviceInfo> GetScannedDevices();

        int Connect(SkineticDevice.OutputType output, System.UInt32 serialNumber);
        int Disconnect();
        SkineticDevice.ConnectionState ConnectionStatus();

        int SetConnectionCallback(SkineticDevice.ConnectionCallbackDelegate callback);

        string GetSDKVersion();
        string GetDeviceVersion();
        System.UInt32 GetDeviceSerialNumber();
        SkineticDevice.DeviceType GetDeviceType();
        int GetGlobalIntensityBoost();
        int SetGlobalIntensityBoost(int globalBoost);
        int LoadPatternFromJSON(string json);
        int UnloadPattern(int patternID);
        int GetPatternIntensityBoost(int patternID);
        int SetAccumulationWindowToPattern(int mainPatternID, int fallbackPatternID, float timeWindow, int maxAccumulation);
        int EraseAccumulationWindowToPattern(int mainPatternID);
        int PlayEffect(int patternID, SkineticDevice.EffectProperties effectProperies);
        int StopEffect(int effectID, float time);
        HapticEffect.State GetEffectState(int effectID);
        int PauseAll();
        int ResumeAll();
        int StopAll();
    }
}
