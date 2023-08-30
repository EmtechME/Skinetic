using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Skinetic
{   
    public class SkineticWrapping : ISkinetic
    {
        [System.Serializable, System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
        public struct CDeviceInfo
        {
            /** Available Output connection mode.*/
            public SkineticDevice.OutputType outputType;
            /** Device Serial Number.*/
            public System.UInt32 serialNumber;
            /** Device Type.*/
            public SkineticDevice.DeviceType deviceType;
            /** Device Version.*/
            [MarshalAs(UnmanagedType.LPStr)] public string deviceVersion;
            /** Pointer to next device.*/
            public IntPtr next;
        }


        private const string DLLNAME = "SkineticSDK";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CallbackDelegate(SkineticDevice.ConnectionState status, int error, UInt32 serialNumber, IntPtr userData);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_createSDKInstance")]
        private static extern int Ski_createSDKInstance([MarshalAs(UnmanagedType.LPStr)] string logFileName);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_freeSDKInstance")]
        private static extern void Ski_freeSDKInstance(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_scanDevices")]
        private static extern int Ski_scanDevices(int sdk_ID, SkineticDevice.OutputType outputType);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_scanStatus")]
        private static extern int Ski_scanStatus(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getFirstScannedDevice")]
        private static extern IntPtr Ski_getFirstScannedDevice(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_connectDevice")]
        private static extern int Ski_connectDevice(int sdk_ID, SkineticDevice.OutputType outputType, UInt32 serialNumber);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_disconnectDevice")]
        private static extern int Ski_disconnectDevice(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_connectionStatus")]
        private static extern SkineticDevice.ConnectionState Ski_connectionStatus(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_setConnectionCallback")]
        private static extern int Ski_setConnectionCallback(int sdk_ID, IntPtr callback, IntPtr userData);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getSDKVersion")]
        private static extern IntPtr Ski_getSDKVersion(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getSkineticSerialNumber")]
        private static extern UInt32 Ski_getSkineticSerialNumber(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getSkineticVersion")]
        private static extern IntPtr Ski_getSkineticVersion(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getSkineticType")]
        private static extern SkineticDevice.DeviceType Ski_getSkineticType(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getGlobalIntensityBoost")]
        private static extern int Ski_getGlobalIntensityBoost(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_setGlobalIntensityBoost")]
        private static extern int Ski_setGlobalIntensityBoost(int sdk_ID, int globalBoost);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_loadPatternFromJSON")]
        private static extern int Ski_loadPatternFromJSON(int sdk_ID, [MarshalAs(UnmanagedType.LPStr)] string json);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_unloadPattern")]
        private static extern int Ski_unloadPattern(int sdk_ID, int patternID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_getPatternIntensityBoost")]
        private static extern int Ski_getPatternIntensityBoost(int sdk_ID, int patternID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_setAccumulationWindowToPattern")]
        private static extern int Ski_setAccumulationWindowToPattern(int sdk_ID, int mainPatternID, int fallbackPatternID, float timeWindow, int maxAccumulation);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_eraseAccumulationWindowToPattern")]
        private static extern int Ski_eraseAccumulationWindowToPattern(int sdk_ID, int mainPatternID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_playEffect")]
        private static extern int Ski_playEffect(int sdk_ID, int patternID, SkineticDevice.EffectProperties effectProperties);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_stopEffect")]
        private static extern int Ski_stopEffect(int sdk_ID, int effectID, float time);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_effectState")]
        private static extern HapticEffect.State Ski_effectState(int sdk_ID, int effectI);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_pauseAll")]
        private static extern int Ski_pauseAll(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_resumeAll")]
        private static extern int Ski_resumeAll(int sdk_ID);

        [global::System.Runtime.InteropServices.DllImport(DLLNAME, EntryPoint = "ski_stopAll")]
        private static extern int Ski_stopAll(int sdk_ID);

        private int m_instanceID = -1;
        private CallbackDelegate m_callbackDelegate = ClassCallback;
        private SkineticDevice.ConnectionCallbackDelegate m_unityDelegate;
        private GCHandle m_handle;

        [AOT.MonoPInvokeCallback(typeof(CallbackDelegate))]
        static public void ClassCallback(SkineticDevice.ConnectionState status, int error, UInt32 serialNumber, IntPtr userData)
        {
            GCHandle obj = GCHandle.FromIntPtr(userData);
            ((SkineticWrapping)obj.Target).InstanceCallback(status, error, serialNumber);
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
            if (m_instanceID != -1)
                return;
            m_instanceID = Ski_createSDKInstance("");
            m_handle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        /// <summary>
        /// Deinitialize the Skinetic device instance.
        /// </summary>
        public void DeinitInstance()
        {
            if (m_instanceID == -1)
                return;
            Ski_freeSDKInstance(m_instanceID);
            m_instanceID = -1;
            m_handle.Free();
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
            return Ski_scanDevices(m_instanceID, output);
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
            return Ski_scanStatus(m_instanceID);
        }

        /// <summary>
        /// This function returns a list of all DeviceInfo of each Skinetic devices found during the scan
        /// which match the specified output type.
        /// </summary>
        /// <returns>a list of DeviceInfo, empty if no match or an error occurs.</returns>
        public List<SkineticDevice.DeviceInfo> GetScannedDevices()
        {
            List<SkineticDevice.DeviceInfo> listDevices = new List<SkineticDevice.DeviceInfo>();
            IntPtr res = Ski_getFirstScannedDevice(m_instanceID);
            if(res == IntPtr.Zero)
            {
                return listDevices;
            }
            CDeviceInfo cdevice = Marshal.PtrToStructure<CDeviceInfo>(Ski_getFirstScannedDevice(m_instanceID));
            Skinetic.SkineticDevice.DeviceInfo device = new SkineticDevice.DeviceInfo();

            device.deviceType = cdevice.deviceType;
            device.deviceVersion = cdevice.deviceVersion;
            device.serialNumber = cdevice.serialNumber;
            device.outputType = cdevice.outputType;
            listDevices.Add(device);
            while (cdevice.next != IntPtr.Zero)
            {
                cdevice = Marshal.PtrToStructure<CDeviceInfo>(cdevice.next);
                device = new SkineticDevice.DeviceInfo();

                device.deviceType = cdevice.deviceType;
                device.deviceVersion = cdevice.deviceVersion;
                device.serialNumber = cdevice.serialNumber;
                device.outputType = cdevice.outputType;
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
            return Ski_connectDevice(m_instanceID, output, serialNumber);
        }

        /// <summary>
        /// Disconnect the current Skinetic device.
        /// The disconnection is effective once all resources are released.
        /// The state of the routine can be obtain from ConnectionStatus().
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int Disconnect()
        {
            return Ski_disconnectDevice(m_instanceID);
        }

        /// <summary>
        /// Check the current status of the connection.
        /// The asynchronous connection routine is terminated on failure.
        /// </summary>
        /// <returns>the current status of the connection.</returns>
        public SkineticDevice.ConnectionState ConnectionStatus()
        {
            return Ski_connectionStatus(m_instanceID);
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
        /// <param name="callback">client's callback</param>
        /// <returns>0 on success, an Error code on failure.</returns>
        public int SetConnectionCallback(SkineticDevice.ConnectionCallbackDelegate callback)
        {
            int ret = Ski_setConnectionCallback(m_instanceID, Marshal.GetFunctionPointerForDelegate(m_callbackDelegate), GCHandle.ToIntPtr(m_handle));
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
            IntPtr ptr = Ski_getSDKVersion(m_instanceID);
            // assume returned string is utf-8 encoded
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// Get the connected device's version as a string. The format of the string is:
        /// <pre>major.minor.revision</pre>
        /// </summary>
        /// <returns>The version string if a Skinetic device is connected, an error message otherwise.</returns>
        public String GetDeviceVersion()
        {
            IntPtr ptr = Ski_getSkineticVersion(m_instanceID);
            // assume returned string is utf-8 encoded
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// Get the connected device's serial number.
        /// </summary>
        /// <returns>The serial number of the connected Skinetic device if any, 0xFFFFFFFF otherwise.</returns>
        public System.UInt32 GetDeviceSerialNumber()
        {
            return Ski_getSkineticSerialNumber(m_instanceID);
        }

        /// <summary>
        /// Get the connected device's type.
        /// </summary>
        /// <returns>The type of the connected Skinetic device if it is connected,
        /// an ERROR message otherwise.</returns>
        public SkineticDevice.DeviceType GetDeviceType()
        {
            return Ski_getSkineticType(m_instanceID);
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
            return Ski_getGlobalIntensityBoost(m_instanceID);
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
            return Ski_setGlobalIntensityBoost(m_instanceID, globalBoost);
        }


        /// <summary>
        /// Load a pattern from a valid json into a local haptic asset and return
        /// the corresponding patternID.The patternID is a positive index.
        /// </summary>
        /// <param name="json">describing the pattern</param>
        /// <returns>Positive patternID on success, an error otherwise.</returns>
        public int LoadPatternFromJSON(String json)
        {
            return Ski_loadPatternFromJSON(m_instanceID, json);
        }

        /// <summary>
        /// Unload the pattern from of the corresponding patternID.
        /// </summary>
        /// <param name="patternID">the patternID of the pattern to unload.</param>
        /// <returns>0 on success, an error otherwise.</returns>
        public int UnloadPattern(int patternID)
        {
            return Ski_unloadPattern(m_instanceID, patternID);
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
            return Ski_getPatternIntensityBoost(m_instanceID, patternID);
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
            return Ski_setAccumulationWindowToPattern(m_instanceID, mainPatternID, fallbackPatternID, timeWindow, maxAccumulation);
        }

        /// <summary>
        /// Disable the effect accumulation strategy on a specific pattern if any set.
        /// </summary>
        /// <param name="mainPatternID">the patternID of the main pattern.</param>
        /// <returns>0 on success, an ERROR otherwise.</returns>
        public int EraseAccumulationWindowToPattern(int mainPatternID)
        {
            return Ski_eraseAccumulationWindowToPattern(m_instanceID, mainPatternID);
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
    public int PlayEffect(int patternID, SkineticDevice.EffectProperties effectProperties)
        {
            return Ski_playEffect(m_instanceID, patternID, effectProperties);
        }

        /// <summary>
        /// Stop the effect instance identified by its effectID.
        /// The effect is stop in "time" seconds with a fade out to prevent abrupt transition. If time
        /// is set to 0, no fadeout are applied and the effect is stopped as soon as possible.
        /// Once an effect is stopped, it is instance is destroyed and its effectID invalidated.
        /// </summary>
        /// <param name="effectID">index identifying the effect.</param>
        /// <param name="time">duration of the fadeout in second.</param>
        /// <returns>0 on success, an error otherwise.</returns>
        public int StopEffect(int effectID, float time)
        {
            return Ski_stopEffect(m_instanceID, effectID, time);
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
            return Ski_effectState(m_instanceID, effectID);
        }

        /// <summary>
        /// Pause all haptic effect that are currently playing.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int PauseAll()
        {
            return Ski_pauseAll(m_instanceID);
        }

        /// <summary>
        /// Resume the paused haptic effects.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int ResumeAll()
        {
            return Ski_resumeAll(m_instanceID);
        }

        /// <summary>
        /// Stop all playing haptic effect.
        /// </summary>
        /// <returns>0 on success, an error otherwise.</returns>
        public int StopAll()
        {
            return Ski_resumeAll(m_instanceID);
        }
    }
}

