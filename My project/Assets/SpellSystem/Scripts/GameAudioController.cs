using System.Collections;
using UnityEngine;

// Drives the intro audio sequence (ambience, thunder, narration) and exposes
// shared SFX hooks. Attach to any persistent GameObject in the scene; clips are
// auto-loaded from Assets/Resources/Audio/ if the inspector slots are empty.
[DefaultExecutionOrder(-100)]
public class GameAudioController : MonoBehaviour
{
    public static GameAudioController Instance { get; private set; }

    [Header("Intro Clips")]
    [SerializeField] AudioClip ambience;
    [SerializeField] AudioClip thunderClap;
    [SerializeField] AudioClip openingSpeech;
    [SerializeField] AudioClip instructionLine;

    [Header("Shared SFX Clips")]
    [SerializeField] AudioClip spellCast;
    [SerializeField] AudioClip castingPrep;

    [Header("Volumes")]
    [Range(0f, 1f)] [SerializeField] float ambienceVolume = 0.18f;
    [Range(0f, 1f)] [SerializeField] float thunderVolume = 1f;
    [Range(0f, 1f)] [SerializeField] float voiceVolume = 1f;

    [Header("Intro Timing (seconds)")]
    [SerializeField] float thunderDelay = 1.5f;
    [SerializeField] float speechMaxWaitAfterThunder = 2f;
    [SerializeField] float instructionDelayAfterSpeech = 6f;
    [SerializeField] bool triggerSpeechOnHeadMove = true;
    [SerializeField] float headMoveAngleThreshold = 8f;

    AudioSource ambienceSource;
    AudioSource voiceSource;
    AudioSource sfxSource;

    public AudioClip SpellCastClip => spellCast;
    public AudioClip CastingPrepClip => castingPrep;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        AutoLoad(ref ambience,        "Audio/tense_suspense_ambience");
        AutoLoad(ref thunderClap,     "Audio/thunder_clap");
        AutoLoad(ref openingSpeech,   "Audio/opening_speech");
        AutoLoad(ref instructionLine, "Audio/instruction_line");
        AutoLoad(ref spellCast,       "Audio/spell_cast");
        AutoLoad(ref castingPrep,     "Audio/casting_prep_sound");

        ambienceSource = CreateSource("Ambience", loop: true);
        voiceSource    = CreateSource("Voice",    loop: false);
        sfxSource      = CreateSource("SFX",      loop: false);
    }

    void Start()
    {
        if (ambience != null)
        {
            ambienceSource.clip = ambience;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(thunderDelay);

        if (thunderClap != null)
            sfxSource.PlayOneShot(thunderClap, thunderVolume);

        // Wait for either a head movement or the max wait window.
        Camera headCam = Camera.main;
        Quaternion startRot = headCam != null ? headCam.transform.rotation : Quaternion.identity;
        float waited = 0f;
        while (waited < speechMaxWaitAfterThunder)
        {
            if (triggerSpeechOnHeadMove && headCam != null &&
                Quaternion.Angle(startRot, headCam.transform.rotation) >= headMoveAngleThreshold)
                break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (openingSpeech != null)
        {
            voiceSource.clip = openingSpeech;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();
            yield return new WaitForSeconds(openingSpeech.length);
        }

        yield return new WaitForSeconds(instructionDelayAfterSpeech);

        if (instructionLine != null)
        {
            voiceSource.clip = instructionLine;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();
        }
    }

    AudioSource CreateSource(string name, bool loop)
    {
        var go = new GameObject($"GameAudio_{name}");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    static void AutoLoad(ref AudioClip slot, string resourcePath)
    {
        if (slot == null) slot = Resources.Load<AudioClip>(resourcePath);
    }
}
