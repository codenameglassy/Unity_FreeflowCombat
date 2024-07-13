using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoaxGames
{
    // Copyright © Kreshnik Halili

    public class EffectsPlayer : MonoBehaviour
    {
        public enum PlayDirection
        {
            RANDOM,
            FORWARD,
            BACKWARD
        }

        public enum PlayAtTarget
        {
            LEFT_FOOT,
            RIGHT_FOOT,
            LEFT_HAND,
            RIGHT_HAND,
            THIS_TRANSFORM_TARGET,
            CUSTOM_TRANSFORM_TARGET,
            LEFT_FOOT_RESULT,
            RIGHT_FOOT_RESULT
        }

        [System.Serializable]
        public abstract class EffectsBase
        {
            [SerializeField] protected bool m_isActiveForPlay = true;
            [SerializeField] protected PlayAtTarget m_playAtTarget = PlayAtTarget.THIS_TRANSFORM_TARGET;
            [SerializeField] protected Transform m_optionalCustomPlayAtTarget;
            [SerializeField] protected Vector3 m_relativePlayAtPositionOffset;
            [SerializeField] protected Vector3 m_relativePlayAtRotationOffset;
            [SerializeField] protected float m_playDelay = 0.0f; public float playDelay { get { return m_playDelay; } }
            [SerializeField] protected InstantiateState m_instantiateState = InstantiateState.UPDATE_ALIGNMENT;
            public enum InstantiateState
            {
                UPDATE_ALIGNMENT,
                INSTANTIATE_AND_FORGET
            }

            public enum CheckLayerAndTagRule
            {
                LAYER_AND_TAG,
                LAYER_OR_TAG,
                LAYER_ONLY,
                TAG_ONLY,
                NONE
            }


            [SerializeField] protected LayerMask m_optionalLayerMask = ~0;
            [SerializeField] protected string m_optionalTriggerTag = "Untagged";
            [SerializeField, Tooltip("If set to LAYER_AND_TAG, both, the layer and the tag have to match. If set to LAYER_OR_TAG, one of the two has to match. If set to NONE, none of the two has to match.")]
            protected CheckLayerAndTagRule m_checkLayerAndTagRule = CheckLayerAndTagRule.LAYER_AND_TAG;

            protected GameObject m_sourceGO;
            protected Transform m_sourceTrans;
            protected Transform m_leftFootTrans;
            protected Transform m_rightFootTrans;
            protected Transform m_leftHandTrans;
            protected Transform m_rightHandTrans;

            protected Transform m_defaultSourceParent;
            protected string m_defaultName;

            protected IKResult m_ikResult;

            public virtual void init(string name, Transform parent)
            {
                initBehaviourBase(name, parent);
            }

            protected void initBehaviourBase(string name, Transform parent)
            {
                m_sourceGO = new GameObject(name);
                m_sourceTrans = m_sourceGO.transform;
                m_sourceTrans.position = parent.position;
                m_sourceTrans.rotation = parent.rotation;

                if (m_instantiateState == InstantiateState.UPDATE_ALIGNMENT) m_sourceTrans.SetParent(parent);

                m_defaultName = name;
                m_defaultSourceParent = parent;

                Animator anim = getComponentInBranch<Animator>(parent);
                m_leftFootTrans = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                m_rightFootTrans = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                m_leftHandTrans = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                m_rightHandTrans = anim.GetBoneTransform(HumanBodyBones.RightHand);

                updateBehaviourBase(true);
            }

            T getComponentInBranch<T>(Transform target)
            {
                T component = target.GetComponentInChildren<T>();
                if(component != null) return component;

                component = target.GetComponentInParent<T>();
                if (component != null) return component;

                while (target.parent != null)
                {
                    target = target.parent;
                    component = target.GetComponentInChildren<T>();
                    if (component != null) return component;
                }

                return target.GetComponentInChildren<T>();
            }

            public virtual void update()
            {
                updateBehaviourBase();
            }

            protected void updateBehaviourBase(bool force = false)
            {
                if (force == false && m_instantiateState == InstantiateState.INSTANTIATE_AND_FORGET)
                {
                    return;
                }

                switch (m_playAtTarget)
                {
                    case PlayAtTarget.CUSTOM_TRANSFORM_TARGET:
                        if (m_optionalCustomPlayAtTarget != null)
                        {
                            m_sourceTrans.rotation = m_optionalCustomPlayAtTarget.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                            m_sourceTrans.position = m_optionalCustomPlayAtTarget.position + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        }
                        break;

                    case PlayAtTarget.THIS_TRANSFORM_TARGET: break;

                    case PlayAtTarget.LEFT_FOOT:
                        if (m_leftFootTrans != null)
                        {
                            m_sourceTrans.rotation = m_leftFootTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                            m_sourceTrans.position = m_leftFootTrans.position + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        }
                        break;

                    case PlayAtTarget.RIGHT_FOOT:
                        if (m_rightFootTrans != null)
                        {
                            m_sourceTrans.rotation = m_rightFootTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                            m_sourceTrans.position = m_rightFootTrans.position + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        }
                        break;

                    case PlayAtTarget.LEFT_HAND:
                        if (m_leftHandTrans != null)
                        {
                            m_sourceTrans.rotation = m_leftHandTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                            m_sourceTrans.position = m_leftHandTrans.position + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        }
                        break;

                    case PlayAtTarget.RIGHT_HAND:
                        if (m_rightHandTrans != null)
                        {
                            m_sourceTrans.rotation = m_rightHandTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                            m_sourceTrans.position = m_rightHandTrans.position + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        }
                        break;

                    case PlayAtTarget.LEFT_FOOT_RESULT:
                        if (m_ikResult == null) break;
                        if (m_ikResult.primaryHitTransform != null) m_sourceTrans.rotation = Quaternion.LookRotation(m_ikResult.normal, m_leftFootTrans.forward) * Quaternion.Euler(m_relativePlayAtRotationOffset);
                        else m_sourceTrans.rotation = m_leftFootTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                        m_sourceTrans.position = m_ikResult.surfacePoint + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        break;

                    case PlayAtTarget.RIGHT_FOOT_RESULT:
                        if (m_ikResult == null) break;
                        if (m_ikResult.primaryHitTransform != null) m_sourceTrans.rotation = Quaternion.LookRotation(m_ikResult.normal, m_rightFootTrans.forward) * Quaternion.Euler(m_relativePlayAtRotationOffset);
                        else m_sourceTrans.rotation = m_rightFootTrans.rotation * Quaternion.Euler(m_relativePlayAtRotationOffset);
                        m_sourceTrans.position = m_ikResult.surfacePoint + m_sourceTrans.rotation * m_relativePlayAtPositionOffset;
                        break;

                    default: break;
                }
            }

            public virtual bool play()
            {
                if (m_isActiveForPlay == false) return false;

                playBehaviourBase();

                return true;
            }

            protected void playBehaviourBase()
            {
                if (m_instantiateState == InstantiateState.INSTANTIATE_AND_FORGET) m_sourceTrans = null;

                if (m_sourceTrans == null)
                {
                    initBehaviourBase(m_defaultName, m_defaultSourceParent);
                }
                else
                {
                    updateBehaviourBase(true);
                }
            }

            public virtual void stop()
            {

            }

            public virtual bool play(IKResult result)
            {
                if (m_isActiveForPlay == false) return false;
                if (result.primaryHitTransform == null) return false;

                bool playEffect = false;

                if (m_checkLayerAndTagRule == CheckLayerAndTagRule.NONE)
                {
                    playEffect = true;
                }
                else if (m_checkLayerAndTagRule == CheckLayerAndTagRule.LAYER_ONLY)
                {
                    playEffect = m_optionalLayerMask == ~0 || m_optionalLayerMask == (m_optionalLayerMask | (1 << result.primaryHitTransform.gameObject.layer));
                }
                else if (m_checkLayerAndTagRule == CheckLayerAndTagRule.TAG_ONLY)
                {
                    playEffect = string.IsNullOrWhiteSpace(m_optionalTriggerTag) || result.primaryHitTransform.CompareTag(m_optionalTriggerTag);
                }
                else
                {
                    bool isLayerOk = m_optionalLayerMask == ~0 || m_optionalLayerMask == (m_optionalLayerMask | (1 << result.primaryHitTransform.gameObject.layer));
                    bool isTagOk = string.IsNullOrWhiteSpace(m_optionalTriggerTag) || result.primaryHitTransform.CompareTag(m_optionalTriggerTag);

                    if (m_checkLayerAndTagRule == CheckLayerAndTagRule.LAYER_AND_TAG)
                    {
                        playEffect = isTagOk && isLayerOk;
                    }
                    else
                    {
                        playEffect = isTagOk || isLayerOk;
                    }
                }

                if (playEffect)
                {
                    m_ikResult = result;
                    playBehaviourBase();
                    return true;
                }

                return false;
            }
        }

        [System.Serializable]
        public class EffectsAudio : EffectsBase
        {
            public EffectsAudio()
            {
                m_optional3DBlendEnd = 100.0f;
                m_audioVolume = 1.0f;
            }
            [SerializeField] protected PlayDirection m_playDirection = PlayDirection.RANDOM;
            [SerializeField, Range(0, 1), Tooltip("0 == fully 2D sound. 1 == fully 3D sound.")] protected float m_2D_3D_Sound_Blend = 1.0f;
            [SerializeField] protected AudioRolloffMode m_audioRolloffMode = AudioRolloffMode.Logarithmic;
            [SerializeField, Range(0, 1), Tooltip("Configure the audio volume with this variable.")] protected float m_audioVolume = 1;
            [SerializeField] protected float m_optional3DBlendStart = 1;
            [SerializeField] protected float m_optional3DBlendEnd = 100.0f;
            [SerializeField] protected List<AudioClip> m_audioClips = new List<AudioClip>();

            protected int m_currentPlayIdx = 0;
            protected AudioSource m_audioSource;

            public override void init(string name, Transform parent)
            {
                m_instantiateState = InstantiateState.UPDATE_ALIGNMENT;

                base.init(name, parent);

                m_audioSource = m_sourceGO.AddComponent<AudioSource>();
                m_audioSource.playOnAwake = false;
            }

            public override bool play()
            {
                m_instantiateState = InstantiateState.UPDATE_ALIGNMENT;

                bool doPlay = base.play();
                if (doPlay == false) return false;
                
                playBehaviour();

                return true;
            }

            protected virtual void playBehaviour()
            {
                if (m_audioClips.Count < 1) return;

                switch (m_playDirection)
                {
                    case PlayDirection.RANDOM:
                        m_currentPlayIdx = Random.Range(0, m_audioClips.Count - 1);
                        break;

                    case PlayDirection.FORWARD:
                        m_currentPlayIdx++;
                        if (m_currentPlayIdx >= m_audioClips.Count) m_currentPlayIdx = 0;
                        break;

                    case PlayDirection.BACKWARD:
                        m_currentPlayIdx--;
                        if (m_currentPlayIdx < 0) m_currentPlayIdx = m_audioClips.Count - 1;
                        break;

                    default: break;
                }

                m_audioSource.volume = m_audioVolume;
                m_audioSource.spatialBlend = m_2D_3D_Sound_Blend;
                m_audioSource.rolloffMode = m_audioRolloffMode;
                m_audioSource.minDistance = m_optional3DBlendStart;
                m_audioSource.maxDistance = m_optional3DBlendEnd;
                m_audioSource.clip = m_audioClips[m_currentPlayIdx];
                m_audioSource.PlayOneShot(m_audioSource.clip, 1);
            }

            public override void stop()
            {
                base.stop();
                stopBehaviour();
            }

            protected virtual void stopBehaviour()
            {
                m_audioSource.Stop();
            }

            public override bool play(IKResult result)
            {
                m_instantiateState = InstantiateState.UPDATE_ALIGNMENT;

                if (base.play(result))
                {
                    playBehaviour();
                    return true;
                }

                return false;
            }
        }

        [System.Serializable]
        public class EffectsInstance : EffectsBase
        {
            [SerializeField] protected bool m_doCacheWithEnableDisableBehaviour = true;
            [SerializeField] protected GameObject m_prefab;

            protected GameObject m_instance;

            public override void init(string name, Transform parent)
            {
                base.init(name, parent);

                if (m_instantiateState == InstantiateState.INSTANTIATE_AND_FORGET && m_sourceGO != null)
                {
                    Destroy(m_sourceGO);
                }
                else
                {
                    if (m_doCacheWithEnableDisableBehaviour)
                    {
                        if (m_instance != null) Destroy(m_instance);
                        if (m_prefab != null)
                        {
                            m_instance = GameObject.Instantiate(m_prefab);
                            m_instance.transform.SetParent(m_sourceTrans);
                            m_instance.transform.localPosition = Vector3.zero;
                            m_instance.transform.localRotation = Quaternion.identity;
                            m_instance.SetActive(false);
                        }
                    }
                }
            }

            public override bool play()
            {
                bool doPlay = base.play();
                if (doPlay == false) return false;

                playBehaviour();

                return true;
            }

            protected virtual void playBehaviour()
            {
                if (m_instantiateState == InstantiateState.INSTANTIATE_AND_FORGET) m_instance = null;

                if (m_doCacheWithEnableDisableBehaviour)
                {
                    if (m_instance == null) m_instance = GameObject.Instantiate(m_prefab);
                    m_instance.transform.SetParent(m_sourceTrans);
                    m_instance.transform.localPosition = Vector3.zero;
                    m_instance.transform.localRotation = Quaternion.identity;
                    m_instance.SetActive(true);
                }
                else
                {
                    if (m_instance != null) Destroy(m_instance);
                    m_instance = GameObject.Instantiate(m_prefab);
                    m_instance.transform.SetParent(m_sourceTrans);
                    m_instance.transform.localPosition = Vector3.zero;
                    m_instance.transform.localRotation = Quaternion.identity;
                }
            }

            public override void stop()
            {
                base.stop();
                stopBehaviour();
            }

            protected virtual void stopBehaviour()
            {
                if (m_doCacheWithEnableDisableBehaviour)
                {
                    if (m_instance != null) m_instance.SetActive(false);
                }
                else
                {
                    if (m_instance != null) Destroy(m_instance);
                }
            }

            public override bool play(IKResult result)
            {
                if (base.play(result))
                {
                    playBehaviour();
                    return true;
                }

                return false;
            }
        }

        [System.Serializable]
        public class TerrainTextureEvents
        {
            [SerializeField] protected List<int> m_filterByTerrainTextureIndexInCaseGroundIsTerrain = new List<int>();
            [SerializeField] protected List<EffectsAudio> m_playAudioClipEvents = new List<EffectsAudio>();
            [SerializeField] protected List<EffectsInstance> m_playInstanceEvents = new List<EffectsInstance>();

            [SerializeField, HideInInspector] protected int m_audioClipEventsCount = 0;
            [SerializeField, HideInInspector] protected int m_instanceEventsCount = 0;

            protected Dictionary<Transform, TerrainTextureDetector> m_terrainTextureDetectors = new Dictionary<Transform, TerrainTextureDetector>();
            protected EffectsPlayer m_target;

            public virtual void init(EffectsPlayer target)
            {
                m_target = target;

#if UNITY_2022_2_OR_NEWER
                Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
                Terrain[] terrains = FindObjectsOfType<Terrain>();
#endif

                foreach (Terrain terrain in terrains)
                {
                    TerrainTextureDetector ttd = terrain.GetComponent<TerrainTextureDetector>();
                    if (ttd == null) ttd = terrain.gameObject.AddComponent<TerrainTextureDetector>();
                    m_terrainTextureDetectors.Add(terrain.transform, ttd);
                }

                foreach (var fx in m_playAudioClipEvents) fx.init("EffectsAudio_Terrain", target.transform);
                foreach (var fx in m_playInstanceEvents) fx.init("EffectsInstance_Terrain", target.transform);
            }

            public virtual void update()
            {
                foreach (var fx in m_playAudioClipEvents) fx.update();
                foreach (var fx in m_playInstanceEvents) fx.update();
            }

            public virtual void play()
            {
                // check if it is a terrain object
                foreach(var ttd in m_terrainTextureDetectors)
                {
                    int index = ttd.Value.GetDominantTextureIndexAt(m_target.transform.position);
                    if (index < 0) continue;

                    if (m_filterByTerrainTextureIndexInCaseGroundIsTerrain.Contains(index))
                    {
                        foreach (var fx in m_playAudioClipEvents) m_target.StartCoroutine(m_target.playDelayedEffects(fx));
                        foreach (var fx in m_playInstanceEvents) m_target.StartCoroutine(m_target.playDelayedEffects(fx));
                        break;
                    }
                }
            }

            public virtual void play(IKResult result)
            {
                // check if it is a terrain object
                if (m_terrainTextureDetectors.ContainsKey(result.primaryHitTransform) == false) return;
                TerrainTextureDetector ttd = m_terrainTextureDetectors[result.primaryHitTransform];
                if (m_filterByTerrainTextureIndexInCaseGroundIsTerrain.Contains(ttd.GetDominantTextureIndexAt(result.surfacePoint)) == false) return;

                foreach (var fx in m_playAudioClipEvents) m_target.StartCoroutine(m_target.playDelayedEffects(fx, result));
                foreach (var fx in m_playInstanceEvents) m_target.StartCoroutine(m_target.playDelayedEffects(fx, result));
            }

            public virtual void stop()
            {
                foreach (var fx in m_playAudioClipEvents) fx.stop();
                foreach (var fx in m_playInstanceEvents) fx.stop();
            }

            public virtual void onValidate()
            {
                // Unity limitation fix that blocks parameters from getting the default values
                if (m_playAudioClipEvents.Count > m_audioClipEventsCount) m_playAudioClipEvents[m_playAudioClipEvents.Count - 1] = new EffectsAudio();
                if (m_playInstanceEvents.Count > m_instanceEventsCount) m_playInstanceEvents[m_playInstanceEvents.Count - 1] = new EffectsInstance();

                m_audioClipEventsCount = m_playAudioClipEvents.Count;
                m_instanceEventsCount = m_playInstanceEvents.Count;
            }
        }

        [SerializeField] protected List<EffectsAudio> m_playAudioClipEvents = new List<EffectsAudio>();
        [SerializeField] protected List<EffectsInstance> m_playInstanceEvents = new List<EffectsInstance>();
        [SerializeField, Tooltip("Can only be used when using the play(IKResult) callback (does not work with play() callback)")] protected List<TerrainTextureEvents> m_playTerrainTextureEvents = new List<TerrainTextureEvents>();

        [SerializeField, HideInInspector] protected int m_audioClipEventsCount = 0;
        [SerializeField, HideInInspector] protected int m_instanceEventsCount = 0;
        [SerializeField, HideInInspector] protected int m_terrainTextureEventsCount = 0;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // Unity limitation fix that blocks parameters from getting the default values
            if (m_playAudioClipEvents.Count > m_audioClipEventsCount) m_playAudioClipEvents[m_playAudioClipEvents.Count - 1] = new EffectsAudio();
            if (m_playInstanceEvents.Count > m_instanceEventsCount) m_playInstanceEvents[m_playInstanceEvents.Count - 1] = new EffectsInstance();
            if (m_playTerrainTextureEvents.Count > m_terrainTextureEventsCount) m_playTerrainTextureEvents[m_playTerrainTextureEvents.Count - 1] = new TerrainTextureEvents();

            foreach (TerrainTextureEvents tte in m_playTerrainTextureEvents) tte.onValidate();

            m_audioClipEventsCount = m_playAudioClipEvents.Count;
            m_instanceEventsCount = m_playInstanceEvents.Count;
            m_terrainTextureEventsCount = m_playTerrainTextureEvents.Count;
        }
#endif

        protected GameObject m_gameObject;

        protected virtual void Awake()
        {
            m_gameObject = this.gameObject;

            foreach (var fx in m_playAudioClipEvents) fx.init("EffectsAudio", this.transform);
            foreach (var fx in m_playInstanceEvents) fx.init("EffectsInstance", this.transform);
            foreach (TerrainTextureEvents tte in m_playTerrainTextureEvents) tte.init(this);
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {

        }

        // Update is called once per frame
        protected virtual void LateUpdate()
        {
            if (m_gameObject == null || m_gameObject.activeInHierarchy == false || this.enabled == false) return;
            foreach (var fx in m_playAudioClipEvents) fx.update();
            foreach (var fx in m_playInstanceEvents) fx.update();

            foreach (var tte in m_playTerrainTextureEvents) tte.update();
        }

        public virtual void play()
        {
            if (m_gameObject == null || m_gameObject.activeInHierarchy == false || this.enabled == false) return;
            foreach (var fx in m_playAudioClipEvents) StartCoroutine(playDelayedEffects(fx));
            foreach (var fx in m_playInstanceEvents) StartCoroutine(playDelayedEffects(fx));

            foreach (var tte in m_playTerrainTextureEvents) tte.play();
        }

        protected virtual IEnumerator playDelayedEffects(EffectsAudio fx)
        {
            if (fx.playDelay > 0.01f) yield return new WaitForSeconds(fx.playDelay);
            fx.play();
        }

        protected virtual IEnumerator playDelayedEffects(EffectsInstance fx)
        {
            if (fx.playDelay > 0.01f) yield return new WaitForSeconds(fx.playDelay);
            fx.play();
        }

        public virtual void stop()
        {
            if (m_gameObject == null || m_gameObject.activeInHierarchy == false || this.enabled == false) return;
            foreach (var fx in m_playAudioClipEvents) fx.stop();
            foreach (var fx in m_playInstanceEvents) fx.stop();

            foreach (var tte in m_playTerrainTextureEvents) tte.stop();
        }

        public virtual void play(IKResult result)
        {
            if (m_gameObject == null || m_gameObject.activeInHierarchy == false || this.enabled == false) return;
            if (result.primaryHitTransform == null) return;

            foreach (var fx in m_playAudioClipEvents) StartCoroutine(playDelayedEffects(fx, result));
            foreach (var fx in m_playInstanceEvents) StartCoroutine(playDelayedEffects(fx, result));

            foreach (var tte in m_playTerrainTextureEvents) tte.play(result);
        }

        protected virtual IEnumerator playDelayedEffects(EffectsAudio fx, IKResult result)
        {
            if (fx.playDelay > 0.01f) yield return new WaitForSeconds(fx.playDelay);
            fx.play(result);
        }

        protected virtual IEnumerator playDelayedEffects(EffectsInstance fx, IKResult result)
        {
            if (fx.playDelay > 0.01f) yield return new WaitForSeconds(fx.playDelay);
            fx.play(result);
        }
    }
}
