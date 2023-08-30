using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinetic
{
    [AddComponentMenu("Skinetic/Haptic Effect")]
    public class HapticEffect : MonoBehaviour
    {
        /// <summary>
        /// Effect's state.
        /// </summary>
        public enum State
        {
            E_PLAY = 2,
            E_MUTE = 1,
            E_INITIALIZED = 0,
            E_STOP = -1
        }

        /// <summary>
        /// Play strategy. 
        /// Defines the haptic effect behavior when PlayEffect() is called. By default, an effect can only play once. While it is playing, any call to PlayEffect() is ignored. 
        /// The force strategy immediatly stops the current playing instance and starts a new one.
        /// The pull strategy starts a new independent instance of the effect at PlayEffect(). When StopEffect() is called 
        /// all pulled effects are simultaneously stopped.
        /// </summary>
        public enum PlayStrategy
        {
            E_DEFAULT = 0,
            E_FORCE,
            E_PULLED
        }

        [SerializeField]
        private PlayStrategy m_strat;
        [SerializeField]
        private PatternAsset m_patternAsset;
        [SerializeField]
        private SkineticDevice m_device;

        [SerializeField]
        private SkineticDevice.EffectProperties m_effectProperties = new SkineticDevice.EffectProperties(5, 100, 1, 1, 0, 0, 0, 0, false, 0, 0, 0, false, false, false, false, false, false);

        /// <summary>
        /// For internal usage.
        /// </summary>
        public List<int> InternalIDs;


        /// <summary>
        /// Defines the haptic effect behavior when PlayEffect() is called. 
        /// By default, an effect can only play once. While it is playing, any call to PlayEffect() is ignore. 
        /// The force strategy immediatly stops the current playing instance and starts a new one.
        /// The pull strategy starts a new independent instance of the effect at PlayEffect(). When StopEffect() is called 
        /// all pulled effects are simultaneously stopped.
        /// </summary>
        public PlayStrategy StrategyOnPlay { get => m_strat; set => m_strat = value; }

        /// <summary>
        /// Level of priority [1; 10] of the effect. In case too many effects are playing simultaneously, the effect with lowest 
        /// priority(10) will be muted.
        /// </summary>
        public int PriorityLevel { get => m_effectProperties.priority; set => m_effectProperties.priority = (int)Mathf.Clamp(value, PropertiesUtils.MIN_PRIORITY, PropertiesUtils.MAX_PRIORITY); }

        /// <summary>
        /// The volume is a percentage of the base volume between [0; 250]%: [0;100[% the pattern attenuated,
        /// 100% the pattern's base volume is preserved, ]100; 250]% the pattern is amplified.
        /// Too much amplification may lead to the clipping of the haptic effects, distorting them
        /// and producing audible noise.
        /// </summary>
        public float Volume { get => m_effectProperties.volume; set => m_effectProperties.volume = Mathf.Clamp(value, PropertiesUtils.MIN_VOLUME, PropertiesUtils.MAX_VOLUME); }
        /// <summary>
        /// The speed is a time scale between [0.01; 100]: 
        /// [0.01; 1[the pattern is slowed down, 
        /// 1 the pattern timing is preserved,
        /// ]1; 100] the pattern is accelerated. 
        /// The resulting speed between the haptic effect's and the samples' speed within the pattern cannot exceed these
        /// bounds. Slowing down or accelerating a sample too much may result in an haptically poor effect.
        /// </summary>
        public float Speed { get => m_effectProperties.speed; set => m_effectProperties.speed = Mathf.Clamp(value, PropertiesUtils.MIN_SPEED, PropertiesUtils.MAX_SPEED); }

        /// <summary>
        /// Number of repetition of the pattern as the effect is playing. If 0, the pattern is repeated indefinitely 
        /// until it is either stopped with stopEffect() or reach the maxDuration value.
        /// </summary>
        public int RepeatCount { get => m_effectProperties.repeatCount; set => m_effectProperties.repeatCount = (int)Mathf.Clamp(value, PropertiesUtils.MIN_REPEATCOUNT, PropertiesUtils.MAX_REPEATCOUNT); }

        /// <summary>
        /// Pause in second between two repetition of the pattern, this value is not affected by the speed parameter.
        /// </summary>
        public float RepeatDelay { get => m_effectProperties.repeatDelay; set => m_effectProperties.repeatDelay = Mathf.Max(value, PropertiesUtils.MIN_REPEATDELAY); }

        /// <summary>
        /// Time in the pattern at which the effect start to play. This value need to be lower than the maxDuration. 
        /// It also takes into account the repeatCount and the repeatDelay of the pattern.
        /// </summary>
        public float PlayAtTime { get => m_effectProperties.playAtTime; set => m_effectProperties.playAtTime = Mathf.Max(value, PropertiesUtils.MIN_PLAYAT); }

        /// <summary>
        /// Maximum duration of the effect, it is automatically stopped if the duration is reached without any 
        /// regards for the actual state of the repeatCount. A maxDuration of 0 remove the duration limit, 
        /// making the effect ables to play indefinitely.
        /// </summary>
        public float MaxDuration { get => m_effectProperties.maxDuration; set => m_effectProperties.maxDuration = Mathf.Max(value, PropertiesUtils.MIN_MAXDURATION); }
        
        /// <summary>
        /// Boost intensity level percent [-100; 100] of the effect to use instead of the
        /// default pattern value if overridePatternBoost is set to true. By using a negative value, can decrease or
        /// even nullify the global intensity boost set by the user.
        /// </summary>
        public int EffectBoost { get => m_effectProperties.effectBoost; set => m_effectProperties.effectBoost = (int)Mathf.Clamp(value, PropertiesUtils.MIN_BOOST, PropertiesUtils.MAX_BOOST); }
        
        /// <summary>
        /// By setting this boolean to true, the effect will use the
        /// effectBoost value instead of the default pattern value.
        /// </summary>
        public bool OverridePatternBoost { get => m_effectProperties.overridePatternBoost; set => m_effectProperties.overridePatternBoost = value; }

        /// <summary>
        /// Height in meter to translate the pattern by.
        /// </summary>
        public float HeightTranslation { get => m_effectProperties.height; set => m_effectProperties.height = value; }

        /// <summary>
        /// Heading angle in degree to rotate the pattern by in the horizontal plan (y axis).
        /// </summary>
        public float HeadingRotation { get => -m_effectProperties.heading; set => m_effectProperties.heading = -value; }

        /// <summary>
        /// Tilting angle in degree to rotate the pattern by in the sagittal plan (x axis).
        /// </summary>
        public float TiltingRotation { get => m_effectProperties.tilting; set => m_effectProperties.tilting = value; }

        /// <summary>
        /// Invert the direction of the pattern on the front-back axis. Can be combine with other
        /// inversion or addition.
        /// </summary>
        public bool FrontBackInversion { get => m_effectProperties.frontBackInversion; set => m_effectProperties.frontBackInversion = value; }

        /// <summary>
        /// Invert the direction of the pattern on the up-down axis. Can be combine with other
        /// inversion or addition.
        /// </summary>
        public bool UpDownInversion { get => m_effectProperties.upDownInversion; set => m_effectProperties.upDownInversion = value; }

        /// <summary>
        /// Invert the direction of the pattern on the right-left axis. Can be combine with other
        /// inversion or addition.
        /// </summary>
        public bool RightLeftInversion { get => m_effectProperties.rightLeftInversion; set => m_effectProperties.rightLeftInversion = value; }

        /// <summary>
        /// Perform a front-back addition of the pattern on the front-back axis. Overrides the
        /// frontBackInversion. Can be combine with other inversion or addition.
        /// </summary>
        public bool FrontBackAddition { get => m_effectProperties.frontBackAddition; set => m_effectProperties.frontBackAddition = value; }

        /// <summary>
        /// Perform a up-down addition of the pattern on the front-back axis. Overrides the
        /// upDownInversion. Can be combine with other inversion or addition.
        /// </summary>
        public bool UpDownAddition { get => m_effectProperties.upDownAddition; set => m_effectProperties.upDownAddition = value; }

        /// <summary>
        /// Perform a right-left addition of the pattern on the front-back axis. Overrides the
        /// rightLeftInversion. Can be combine with other inversion or addition.
        /// </summary>
        public bool RightLeftAddition { get => m_effectProperties.rightLeftAddition; set => m_effectProperties.rightLeftAddition = value; }

        /// <summary>
        /// Structure containing all properties of an haptic effect instance. The HapticEffect component has a base 
        /// EffectProperties structure that can be override.
        /// </summary>
        public SkineticDevice.EffectProperties Properties { get => m_effectProperties; set => m_effectProperties = value; }

        /// <summary>
        /// Device to which the effect will be played on.
        /// </summary>
        public PatternAsset TargetPattern { get => m_patternAsset; set => m_patternAsset = value; }

        /// <summary>
        /// PatternAsset from which the effect will be instantiated. A default scriptable object can be set in the inspector. 
        /// However, it can also be set at runtime through scripting. Notice that the target device should have already loaded 
        /// the PatternAsset before the HapticEffect can be played.
        /// </summary>
        public SkineticDevice TargetDevice { get => m_device; set => m_device = value; }

        private void Start()
        {
            InternalIDs = new List<int>();
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
        /// <returns>true on success, false otherwise.</returns>
        public bool PlayEffect()
        {
            if (m_device == null)
                return false;

            if (m_patternAsset == null)
                return false;

            return m_device.PlayEffect(this);
        }

        /// <summary>
        /// Stop the effect instance.
        /// The effect is stop in "time" seconds with a fade out to prevent abrupt transition. If time
        /// is set to 0, no fadeout are applied and the effect is stopped as soon as possible.
        /// </summary>
        /// <param name="time">duration of the fadeout in seconds.</param>
        /// <returns>true on success, false otherwise.</returns>
        public bool StopEffect(float time)
        {
            if(m_device == null)
                return false;
            if(m_patternAsset == null)
                return false;
            return m_device.StopEffect(this, time);
        }

        /// <summary>
        /// Get the current state of an effect. If the haptic effect is invalid, the 'stop' state
        /// will be return.
        /// </summary>
        /// <returns>the current state of the effect.</returns>
        public State GetEffectState()
        {
            if (m_device == null)
                return State.E_STOP;

            if (m_patternAsset == null)
                return State.E_STOP;

            return m_device.GetEffectState(this);
        }
    }
}

