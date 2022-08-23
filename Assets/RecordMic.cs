using FMODUnity;
using UnityEngine;
using System.Collections;

// Code Source: https://www.youtube.com/watch?v=Q-ivRHMdJ2c&ab_channel=ScottGameSounds
// Title: FMOD & Unity | Recording The Players Voice And Playing It Back At Runtime
// Author: S

enum State 
{
    START,
    RECORDING,
    PLAYBACK,
    OVERDUBBING,
    OVERDUBBING_AWAIT
}

public class RecordMic : MonoBehaviour
{
    //public variables
    [Header("Choose A Microphone")]
    public int RecordingDeviceIndex = 0;
    [TextArea] public string RecordingDeviceName = null;
    [Header("How Long In Seconds Before Recording Plays")]
    public float Latency = 1f;
    [Header("Choose A Key To Play/Pause/Add Reverb To Recording")]
    public KeyCode PlayAndPause;
    public KeyCode ReverbOnOffSwitch;

    public KeyCode StartRecordingKey;
    public KeyCode StopRecordingKey;
    public KeyCode StartOverdubKey;

    public uint MaxLoopDuration = 10;
    public uint MaxLoopLayers = 10;

    //FMOD Objects
    private FMOD.Sound sound;
    private FMOD.Sound[] sounds;
    private FMOD.CREATESOUNDEXINFO exinfo;
    private FMOD.Channel channel;
    private FMOD.ChannelGroup channelGroup;

    //How many recording devices are plugged in for us to use.
    private int numOfDriversConnected = 0;
    private int numofDrivers = 0;

    //Info about the device we're recording with.
    private System.Guid MicGUID;
    private int SampleRate = 0;
    private FMOD.SPEAKERMODE FMODSpeakerMode;
    private int NumOfChannels = 0;
    private FMOD.DRIVER_STATE driverState;
    
    //Other variables.
    private bool dspEnabled = false;
    private bool playOrPause = true;
    private bool playOkay = false;

    private State state = State.START;

    private bool playbackStarted = false; 

    private float loopDuration = 0f;

    private float loopTracker = 0f; // Tracks what point in time we are in the loop, used for overdubbing
    private int currNumSounds = 0; // How many sounds we have recorded so far

    private float latency;  // Testing performance: variable for checking latency 

    private float _sameLoopVar = 0f;

    void Start()
    {
        //Step 1: Check to see if any recording devices (or drivers) are plugged in and available for us to use.


        RuntimeManager.CoreSystem.getRecordNumDrivers(out numofDrivers, out numOfDriversConnected);

        if (numOfDriversConnected == 0)
            Debug.Log("Hey! Plug a Microhpone in ya dummy!!!");
        else
            Debug.Log("You have " + numOfDriversConnected + " microphones available to record with.");


        //Step 2: Get all of the information we can about the recording device (or driver) that we're
        //        going to use to record with.


        RuntimeManager.CoreSystem.getRecordDriverInfo(RecordingDeviceIndex, out RecordingDeviceName, 50,
            out MicGUID, out SampleRate, out FMODSpeakerMode, out NumOfChannels, out driverState);


        //Next we want to create an "FMOD Sound Object", but to do that, we first need to use our 
        //FMOD.CREATESOUNDEXINFO variable to hold and pass information such as the sample rate we're
        //recording at and the num of channels we're recording with into our Sound object.


        //Step 3: Store relevant information into FMOD.CREATESOUNDEXINFO variable.
        

        exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exinfo.numchannels = NumOfChannels;
        exinfo.format = FMOD.SOUND_FORMAT.PCM16;
        exinfo.defaultfrequency = SampleRate;
        exinfo.length = (uint)SampleRate * sizeof(short) * (uint)NumOfChannels * MaxLoopDuration;


        //Step 4: Create an FMOD Sound "object". This is what will hold our voice as it is recorded.

        // List of sounds
        sounds = new FMOD.Sound[MaxLoopLayers];
        for (int i = 0; i < MaxLoopLayers; i++) {
            RuntimeManager.CoreSystem.createSound(exinfo.userdata, FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER,
                ref exinfo, out sounds[i]);
        }

        RuntimeManager.CoreSystem.createSound(exinfo.userdata, FMOD.MODE.LOOP_NORMAL | FMOD.MODE.OPENUSER, 
            ref exinfo, out sound);


        //Step 5: Start recording through our chosen device into our Sound object.


        // RuntimeManager.CoreSystem.recordStart(RecordingDeviceIndex, sound, true);


        // Step 6: Start a Corutine that will tell our Sound object to play after a ceratin amount of time.


        // StartCoroutine(Wait());
    }


    // IEnumerator Wait()
    // {
    //     yield return new WaitForSeconds(Latency);
    //     RuntimeManager.CoreSystem.playSound(sound, channelGroup, true, out channel);
    //     playOkay = true;
    //     Debug.Log("Ready To Play");
    // }

    // Begin recording audio
    void StartRecording(int soundObjectIndex) {
        RuntimeManager.CoreSystem.recordStart(RecordingDeviceIndex, sounds[soundObjectIndex], false);
    }

    void StopRecording () {
        RuntimeManager.CoreSystem.recordStop(RecordingDeviceIndex);
    }

    void trackLoop() {
        
    }



    void Update() {
        switch(state)
        {
            case State.START:
                if (Input.GetKeyDown(StartRecordingKey)) {
                    StartRecording(0);
                    state = State.RECORDING;
                    Debug.Log("Recording Started");
                } break;
            
            case State.RECORDING:
                loopDuration += Time.deltaTime;
                if (Input.GetKey(StopRecordingKey)) {
                    latency = Time.time; // check for 
                    StopRecording();
                    Debug.Log("Recording Stopped");
                    Debug.Log("Loop Duration (sec): " + loopDuration);

                    
                    // set loop duration for all sounds objects
                    for (int i = 0; i < MaxLoopLayers; i++)
                        sounds[i].setLoopPoints(0, FMOD.TIMEUNIT.MS, (uint)(loopDuration * 1000), FMOD.TIMEUNIT.MS);

                    // Play first sound
                    RuntimeManager.CoreSystem.playSound(sounds[0], channelGroup, false, out channel);
                    loopTracker = 0f;
                    currNumSounds += 1;

                    Debug.Log("Latency (ms) : " + (Time.time - latency) * 1000);
                    Debug.Log("Playback Started");

                    state = State.PLAYBACK;
                } 
                break;
            
            // Playing back audio, not overdubbing
            case State.PLAYBACK:
                loopTracker = (loopTracker + Time.deltaTime) % loopDuration;
                if (Input.GetKey(StartOverdubKey)) {
                    Debug.Log("Begin awaiting to overdub");
                    state = State.OVERDUBBING_AWAIT;
                } break;

            // Playing back audio, waiting for the next loop to begin overdubbing
            case State.OVERDUBBING_AWAIT:
                loopTracker = (loopTracker + Time.deltaTime) % loopDuration;
                // if within 25ms of loopBegin
                if ((loopTracker + 0.025) % loopDuration < 0.050) {
                    // start recording next sound
                    StartRecording(currNumSounds);
                    Debug.Log("Begin overdubbing");

                    state = State.OVERDUBBING;
                    _sameLoopVar = 0f;  // variable to track current loop
                } break;
        
            case State.OVERDUBBING:
                loopTracker = (loopTracker + Time.deltaTime) % loopDuration;
                _sameLoopVar += Time.deltaTime; // make sure we aren't recording and stopping in the same loop
                if ((loopTracker + 0.025) % loopDuration < 0.050 && _sameLoopVar > 0.5) {
                    StopRecording();

                    // Play recorded sound
                    RuntimeManager.CoreSystem.playSound(sounds[currNumSounds], channelGroup, false, out channel);
                    currNumSounds += 1;

                    Debug.Log("Overdubbing Finished");
                    state = State.PLAYBACK;
                }
                break;

            default:
                break;
        }





        // if (Input.GetKeyDown(PlayAndPause) && playOkay)
        // {
        //     playOrPause = !playOrPause;
        //     channel.setPaused(playOrPause);
        // }
    }




    // void Update()
    // {
    //     //Step 7: Once the Coroutine has started playing our sound, we check to see if a particular button has
    //     //        been pressed and if it has, then we pause or unpasue the channel that the Sound object is
    //     //        playing through.


    //     if (Input.GetKeyDown(PlayAndPause) && playOkay)
    //     {
    //         playOrPause = !playOrPause;
    //         channel.setPaused(playOrPause);
    //     }


    //     //Optional
    //     //Step 8: Set a reverb to the Sound object we're recording into and turn it on or off with a new button.


    //     if (Input.GetKeyDown(ReverbOnOffSwitch))
    //     {
    //         FMOD.REVERB_PROPERTIES propOn = FMOD.PRESET.ROOM();
    //         FMOD.REVERB_PROPERTIES propOff = FMOD.PRESET.OFF();

    //         dspEnabled = !dspEnabled;

    //         RuntimeManager.CoreSystem.setReverbProperties(1, ref dspEnabled ? ref propOn : ref propOff);
    //     }

    // }
}