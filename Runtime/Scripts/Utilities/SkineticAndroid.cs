using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Skinetic
{
    public class SkineticAndroid : ISkinetic
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CallbackDelegate(SkineticDevice.ConnectionState status, int error, UInt32 serialNumber, IntPtr userData);

        private CallbackDelegate m_callbackDelegate = ClassCallback;
        private SkineticDevice.ConnectionCallbackDelegate m_unityDelegate;
        private GCHandle m_handle;
        private IntPtr m_structPtr;

        private bool m_init = false;

        private static AndroidJavaClass enumOutputTypeJavaClass;
        private static AndroidJavaClass enumDeviceTypeJavaClass;
        private static AndroidJavaClass unityPlayer;

        private AndroidJavaObject m_activity;
        private AndroidJavaObject m_context;
        private AndroidJavaObject m_skineticObj;

        [AOT.MonoPInvokeCallback(typeof(CallbackDelegate))]
        static public void ClassCallback(SkineticDevice.ConnectionState status, int error, UInt32 serialNumber, IntPtr userData)
        {
            GCHandle obj = GCHandle.FromIntPtr(userData);
            ((SkineticAndroid)obj.Target).InstanceCallback(status, error, serialNumber);
        }

        private void InstanceCallback(SkineticDevice.ConnectionState status, int error, UInt32 serialNumber)
        {
            m_unityDelegate(status, error, serialNumber);
        }

        /// <summary>
        /// Initialize the Skinetic device instance.
        /// </summary>
        public void InitInstance()
        {
            if (m_init)
                return;
            Debug.Log("Get java variables");
            enumOutputTypeJavaClass = new AndroidJavaClass("com.actronika.skineticsdk.SkineticSDK$OUTPUT_TYPE");
            enumDeviceTypeJavaClass = new AndroidJavaClass("com.actronika.skineticsdk.SkineticSDK$DEVICE_TYPE");
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            m_activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            m_context = m_activity.Call<AndroidJavaObject>("getApplicationContext");
            m_skineticObj = new AndroidJavaObject("com.actronika.skineticsdk.SkineticSDK", m_context, m_activity);

            Debug.Log("Allocate delegate handle");
            m_handle = GCHandle.Alloc(this, GCHandleType.Normal);
            Debug.Log("Allocate delegate handle done");

            SkineticDevice.EffectProperties test = new SkineticDevice.EffectProperties();
            m_structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(test));
        }

        /// <summary>
        /// Deinitialize the Skinetic device instance.
        /// </summary>
        public void DeinitInstance()
        {
            if (!m_init)
                return;
            m_activity.Dispose();
            m_context.Dispose();
            m_skineticObj.Dispose();

            m_handle.Free();
            Marshal.FreeHGlobal(m_structPtr);
        }
        
        
       

        /// <summary>
        /// Initialize a scanning routine to find all available Skinetic device.
        /// the state of the routine can be obtain from ScanStatus(). Once completed,
        /// the result can be accessed using GetScannedDevices().
        /// </summary>
        /// <param name="output"></param>
        /// <returns>0 on success, an Error code on failure.</returns>
        public int ScanDevices(SkineticDevice.OutputType output)
        {
            AndroidJavaObject enumObj = enumOutputTypeJavaClass.GetStatic<AndroidJavaObject>(output.ToString());
            return m_skineticObj.Call<int>("scanDevices", enumObj);
        }

        /// <summary>
        /// Check the status of the asynchronous scanning routine.
        /// The method returns:
        ///  - 1 if the scan is ongoing.
        ///  - 0 if the scan is completed.
        ///  - a negative error code if the connection failed.
        /// The asynchronous scan routine is terminated on failure.
        /// Once the scan is completed, the result can be obtain by calling GetScannedDevices().
        /// </summary>
        /// <returns>the current status or an error on failure.</returns>
        public int ScanStatus()
        {
            return m_skineticObj.Call<int>("scanStatus");
        }

        /// <summary>
        /// This function returns a list of all DeviceInfo of each Skinetic devices found during the scan
        /// which match the specified output type.
        /// </summary>
        /// <returns>a list of DeviceInfo, empty if no match or an error occurs.</returns>
        public  List<SkineticDevice.DeviceInfo> GetScannedDevices()
        {
            AndroidJavaObject jlistDevices = m_skineticObj.Call<AndroidJavaObject>("getScannedDevices");
            int count = jlistDevices.Call<int>("size");
            List<SkineticDevice.DeviceInfo> listDevices = new List<SkineticDevice.DeviceInfo>();

            for (int i = 0; i < count; i++)
            {
                SkineticDevice.DeviceInfo device = new SkineticDevice.DeviceInfo();
                AndroidJavaObject jdeviceInfo = jlistDevices.Call<AndroidJavaObject>("get", i);

                device.serialNumber = (UInt32)jdeviceInfo.Get<long>("serialNumber");

                AndroidJavaObject enumObj = jdeviceInfo.Get<AndroidJavaObject>("outputType");
                device.outputType = (SkineticDevice.OutputType)enumObj.Call<int>("getValue");

                enumObj = jdeviceInfo.Get<AndroidJavaObject>("deviceType");
                device.deviceType = (SkineticDevice.DeviceType)enumObj.Call<int>("getValue");

                device.deviceVersion = jdeviceInfo.Get<String>("deviceVersion");
                listDevices.Add(device);
            }
            return listDevices;
        }

        /// <summary>
        /// Initialize an asynchronous connection to a Skinetic device using the selected type of connection.
        /// The state of the routine can be obtain from ConnectionStatus().
        /// If the serial number is set to 0, the connection will be performed on the first found device.
        /// </summary>
        /// <param name="output">output type</param>
        /// <param name="serialNumber">serial number of the Skinetic device to connect to</param>
        /// <returns>0 on success, an error otherwise.</returns>
        public int Connect(SkineticDevice.OutputType output, System.UInt32 serialNumber)
        {
            AndroidJavaObject enumObj = enumOutputTypeJavaClass.GetStatic<AndroidJavaObject>(output.ToString());       
            return m_skineticObj.Call<int>("connect", enumObj, (Int64)serialNumber);
        }

        /// <summary>
        /// Disconnect the current Skinetic device.
        /// The disconnection is effective once all resources are released.
        /// The state of the routine can be obtain from ConnectionStatus().
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int Disconnect()
        {
            return m_skineticObj.Call<int>("disconnect");
        }

        /// <summary>
        /// Check the current status of the connection.
        /// The asynchronous connection routine is terminated on failure.
        /// </summary>
        /// <returns>the current status of the connection.</returns>
        public SkineticDevice.ConnectionState ConnectionStatus()
        {
            AndroidJavaObject enumObj = m_skineticObj.Call<AndroidJavaObject>("connectionStatus");
            return (SkineticDevice.ConnectionState)enumObj.Call<int>("getValue");
        }


        /// <summary>
        /// Set a callback function fired upon connection changes.
        /// Functions of type ski_ConnectionCallback are implemented by clients.
        /// The callback is fired at the end of the connection routine weither it succeed
        /// or failed.It is also fired if a connection issue arise.
        /// The callback is not fired if none was passed to setConnectionCallback().
        /// userData is a client supplied pointer which is passed back when the callback
        /// function is called.It could for example, contain a pointer to an class instance
        /// that will process the callback.
        /// </summary>
        /// <param name="callback">callback client's callback</param>
        /// <returns>0 on success, an Error code on failure.</returns>
        public int SetConnectionCallback(SkineticDevice.ConnectionCallbackDelegate callback)
        {
            int ret = m_skineticObj.Call<int>("setConnectionCallback", (Int64)Marshal.GetFunctionPointerForDelegate(m_callbackDelegate), (Int64)GCHandle.ToIntPtr(m_handle));
            if (ret == 0)
                m_unityDelegate = callback;
            return ret;
        }



        /// <summary>
        /// Get SDK version as a string. The format of the string is:
        /// <pre>major.minor.revision</pre>
        /// </summary>
        /// <returns>The version string.</returns>
        public String GetSDKVersion()
        {
            return m_skineticObj.Call<String>("getSDKVersion");
        }

        /// <summary>
        /// Get the connected device's version as a string. The format of the string is:
        /// <pre>major.minor.revision</pre>
        /// </summary>
        /// <returns>The version string if a Skinetic device is connected, an error message otherwise.</returns>
        public String GetDeviceVersion()
        {
            return m_skineticObj.Call<String>("getDeviceVersion");
        }

        /// <summary>
        /// Get the connected device's serial number.
        /// </summary>
        /// <returns>The serial number of the connected Skinetic device if any, 0xFFFFFFFF otherwise.</returns>
        public System.UInt32 GetDeviceSerialNumber()
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(m_skineticObj.Call<long>("getDeviceSerialNumber")), 0);
        }

        /// <summary>
        /// Get the connected device's type.
        /// </summary>
        /// <returns>The type of the connected Skinetic device if it is connected,
        /// an ERROR message otherwise.</returns>
        public SkineticDevice.DeviceType GetDeviceType()
        {
            AndroidJavaObject enumObj = m_skineticObj.Call<AndroidJavaObject>("getDeviceType");
            return (SkineticDevice.DeviceType)enumObj.Call<int>("getValue");
        }

        /// <summary>
        /// Get the amount of effect's intensity boost.
        /// The boost increase the overall intensity of all haptic effects.
        /// However, the higher the boost activation is, the more the haptic effects are degraded.
        /// The global boost is meant to be set by the user as an application setting.
        /// </summary>
        /// <returns>The percentage of effect's intensity boost, an ERROR otherwise.</returns>
        public int GetGlobalIntensityBoost()
        {
            return m_skineticObj.Call<int>("getGlobalIntensityBoost");
        }

        /// <summary>
        /// Set the amount of global intensity boost.
        /// The boost increase the overall intensity of all haptic effects.
        /// However, the higher the boost activation is, the more the haptic effects are degraded.
        /// The global boost is meant to be set by the user as an application setting.
        /// </summary>
        /// <param name="globalBoost">boostPercent percentage of the boost.</param>
        /// <returns>0 on success, an ERROR otherwise.</returns>
        public int SetGlobalIntensityBoost(int globalBoost)
        {
            return m_skineticObj.Call<int>("setGlobalIntensityBoost", globalBoost);
        }

        /// <summary>
        /// Load a pattern from a valid json into a local haptic asset and return
        /// the corresponding patternID.The patternID is a positive index.
        /// </summary>
        /// <param name="json">describing the pattern</param>
        /// <returns>Positive patternID on success, an error otherwise.</returns>
        public int LoadPatternFromJSON(String json)
        {
            return m_skineticObj.Call<int>("loadPatternFromJSON", json);
        }

        /// <summary>
        /// Unload the pattern from of the corresponding patternID.
        /// </summary>
        /// <param name="patternID">the patternID of the pattern to unload.</param>
        /// <returns>0 on success, an error otherwise.</returns>
        public int UnloadPattern(int patternID)
        {
            return m_skineticObj.Call<int>("unloadPattern", patternID);
        }

        /// <summary>
        ///Get the pattern boost value which serves as a default value for the playing effect. 
        ///The value is ranged in [-100; 100]. 
        ///If the pattern ID is invalid, zero is still returned.
        /// </summary>
        /// <param name="patternID">the ID of the targeted pattern.</param>
        /// <returns>the pattern intensity boost of the pattern if it exists, 0 otherwise.</returns>
        public int GetPatternIntensityBoost(int patternID)
        {
            return m_skineticObj.Call<int>("getPatternIntensityBoost", patternID);
        }

        /// <summary>
        /// Enable the effect accumulation strategy: Whenever an effect is triggered on the primary pattern,
        /// the fallback one is used instead.
        /// The purpose of this feature is to enable the use of a lighter pattern if a specific pattern might
        /// be called too many time.Instead of triggering the mute strategy several time in a row, it enables
        /// the main effect to be rendered fully while having another lighter being triggered and still providing
        /// the additional event.
        /// 
        /// The accumulation is done on the specified time window, starting with the first call. Each subsequent call
        /// within this time window will trigger the fallback pattern and increase the count until the maxAccumulation
        /// value is reach. Additional call will then be ignore until the time window is finished.
        /// If the subsequent call have the same or higher priority, the actual priority is set to be just below the main
        /// effect, as to preserve the unity of the accumulation with respect to other playing effects. However, if the
        /// subsequent calls have lower priorities, then the standard priority system is used.
        /// 
        /// If a new call to this function is done for a specific pattern, the previous association is overridden.
        /// </summary>
        /// <param name="mainPatternID">the patternID of the main pattern.</param>
        /// <param name="fallbackPatternID">the patternID of the fallback pattern</param>
        /// <param name="timeWindow">the time window during which the accumulation should happen.</param>
        /// <param name="maxAccumulation">max number of extra accumulated effect instances.</param>
        /// <returns>0 on success, an ERROR otherwise.</returns>
        public int SetAccumulationWindowToPattern(int mainPatternID, int fallbackPatternID, float timeWindow, int maxAccumulation)
        {
            return m_skineticObj.Call<int>("setAccumulationWindowToPattern", mainPatternID, fallbackPatternID, timeWindow, maxAccumulation);
        }

        /// <summary>
        /// Disable the effect accumulation strategy on a specific pattern if any set.
        /// </summary>
        /// <param name="mainPatternID">the patternID of the main pattern.</param>
        /// <returns>0 on success, an ERROR otherwise.</returns>
        public int EraseAccumulationWindowToPattern(int mainPatternID)
        {
            return m_skineticObj.Call<int>("eraseAccumulationWindowToPattern", mainPatternID);
        }

        /// <summary>
        /// Play an haptic effect based on a loaded pattern and return the effectID of this instance.
        /// The instance index is positive.Each call to playEffect() using the same patternID
        /// generates a new haptic effect instance totally uncorrelated to the previous ones.
        /// The instance is destroyed once it stops playing.
        /// 
        /// The haptic effect instance reproduces the pattern with variations describes in the structure
        /// ski_effect_properties_t.More information on these parameters and how to used them can be found
        /// in the structure's description.
        /// 
        /// If the pattern is unloaded, the haptic effect is not interrupted.
        /// </summary>
        /// <param name="patternID">pattern used by the effect instance.</param>
        /// <param name="effectProperties">struct to specialized the effect.</param>
        /// <returns>Positive effectID on success, an error otherwise.</returns>
        public int PlayEffect(int patternID, SkineticDevice.EffectProperties effectProperies)
        {
            Marshal.StructureToPtr(effectProperies, m_structPtr, false);
            return m_skineticObj.Call<int>("playEffectByPtr", patternID, (Int64)m_structPtr);
        }

        /// <summary>
        /// Stop the effect instance identified by its effectID.
        /// The effect is stop in "time" seconds with a fade out to prevent abrupt transition. If time
        /// is set to 0, no fadeout are applied and the effect is stopped as soon as possible.
        /// Once an effect is stopped, it is instance is destroyed and its effectID invalidated.
        /// </summary>
        /// <param name="effectID">index identifying the effect.</param>
        /// <param name="time">duration of the fadeout in seconds.</param>
        /// <returns>0 on success, an error otherwise.</returns>
        public int StopEffect(int effectID, float time)
        {
            return m_skineticObj.Call<int>("stopEffect", effectID, time);
        }

        /// <summary>
        ///  Get the current state of an effect. If the effectID is invalid, the 'stop' state
        ///  will be return.
        ///
        /// @param effectID index identifying the effect.
        /// @return the current state of the effect.
        /// </summary>
        public HapticEffect.State GetEffectState(int effectID)
        {
            AndroidJavaObject enumObj = m_skineticObj.Call<AndroidJavaObject>("effectState", effectID);
            return (HapticEffect.State)enumObj.Call<int>("getValue");
        }

        /// <summary>
        /// Pause all haptic effect that are currently playing.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int PauseAll()
        {
            return m_skineticObj.Call<int>("pauseAll");
        }

        /// <summary>
        /// Resume the paused haptic effects.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int ResumeAll()
        {
            return m_skineticObj.Call<int>("resumeAll");
        }

        /// <summary>
        /// Stop all playing haptic effect.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int StopAll()
        {
            return m_skineticObj.Call<int>("stopAll");
        }
    }
}