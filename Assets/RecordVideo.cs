using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Windows.WebCam;
using UnityEngine.Video;

public class RecordVideo : MonoBehaviour
{
    static readonly float MaxRecordingTime = 5.0f;

    [Header("Choose A Key To Begin the Video Recording")]
    public KeyCode BeginRecordKey;

    [Header("Choose A Key To Begin Overdubbing")]
    public KeyCode BeginOverdubKey;

    VideoCapture m_VideoCapture = null;

    CameraParameters m_CameraParameters;
    float m_stopRecordingTimer = float.MaxValue;
    
    [SerializeField]
    private string filepath;
    public GameObject videoScreen1;
    public GameObject videoScreen2;
    public GameObject videoScreen3;
    public GameObject videoScreen4;
    public GameObject videoScreen5;

    private int videoCount = 0;

    private float latency;

    State state = State.START;

    private float loopTracker;

    private float _sameLoopVar = 0f;

    // Use this for initialization
    void Start()
    {
        prepareVideoCapture();
    }

    async void Update()
    {
        // if (m_VideoCapture == null || !m_VideoCapture.IsRecording)
        // {
        //     return;
        // }

        switch(state) {
            case State.START:
                if (Input.GetKey(BeginRecordKey)) {
                    latency = Time.time;

                    StartVideoCaptureTest();
                    state = State.RECORDING;

                    Debug.Log("Delay before recording: " + (Time.time - latency) * 1000);
                } break;

            case State.RECORDING:
                if (Time.time > m_stopRecordingTimer)
                {
                    latency = Time.time;

                    m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
                    
                    state = State.PLAYBACK;

                    loopTracker = 0f;

                } break;

            case State.PLAYBACK:
                loopTracker = (loopTracker + Time.deltaTime) % MaxRecordingTime;
                if (Input.GetKey(BeginOverdubKey)) {
                    state = State.OVERDUBBING_AWAIT;
                    Debug.Log("Begin awaiting to overdub");
                } break;

            case State.OVERDUBBING_AWAIT:
                loopTracker = (loopTracker + Time.deltaTime) % MaxRecordingTime;
                // if within 25ms of loopBegin
                if ((loopTracker + 0.025) % MaxRecordingTime < 0.040) {
                    // start recording next video
                    StartVideoCaptureTest();
                    Debug.Log("Begin overdubbing");

                    state = State.OVERDUBBING;
                    _sameLoopVar = 0f;  // variable to track current loop
                } break;

            case State.OVERDUBBING:
                loopTracker = (loopTracker + Time.deltaTime) % MaxRecordingTime;
                _sameLoopVar += Time.deltaTime; // make sure we aren't recording and stopping in the same loop
                if ((loopTracker + 0.025) % MaxRecordingTime < 0.040 && _sameLoopVar > 0.5) {
                    latency = Time.time;

                    m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
                    state = State.PLAYBACK;

                    Debug.Log("Overdubbing Finished");

                    // Debug.Log("Delay before playing: " + (Time.time - latency) * 1000);

                } break;


            default: break;
        }


    }

    void StartVideoCaptureTest() {
        m_VideoCapture.StartVideoModeAsync(m_CameraParameters,
            VideoCapture.AudioState.ApplicationAndMicAudio,
            OnStartedVideoCaptureMode);
    }

    void prepareVideoCapture()
    {
        Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Debug.Log(cameraResolution);

        float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
        Debug.Log(cameraFramerate);

        VideoCapture.CreateAsync(false, delegate(VideoCapture videoCapture)
        {
            if (videoCapture != null)
            {
                m_VideoCapture = videoCapture;
                Debug.Log("Created VideoCapture Instance!");

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                m_CameraParameters = cameraParameters;
                // m_VideoCapture.StartVideoModeAsync(cameraParameters,
                //     VideoCapture.AudioState.ApplicationAndMicAudio,
                //     OnStartedVideoCaptureMode);
            }
            else
            {
                Debug.LogError("Failed to create VideoCapture Instance!");
            }
        });
    }

    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Video Capture Mode!");
        string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
        string filename = string.Format("TestVideo_{0}.mp4", timeStamp);
        // string filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        filepath = System.IO.Path.Combine(Application.temporaryCachePath, filename);
        filepath = filepath.Replace("/", @"\");
        m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
    }

    void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Video Capture Mode!");
    }

    void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Recording Video!");
        m_stopRecordingTimer = Time.time + MaxRecordingTime;
    }

    void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Recording Video!");
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);

        videoCount += 1;
        switch(videoCount) {
            case 1: beginPlayback(videoScreen1); break;
            case 2: beginPlayback(videoScreen2); break;
            case 3: beginPlayback(videoScreen3); break;
            case 4: beginPlayback(videoScreen4); break;
            case 5: beginPlayback(videoScreen5); break;
            default: break;
        }
        Debug.Log("Delay before playing: " + (Time.time - latency) * 1000);
    }

    void beginPlayback(GameObject videoScreen) {

        VideoPlayer vp = videoScreen.GetComponent<VideoPlayer>();
        if (!vp) Debug.Log("ERROR: CAN'T FIND VIDEO PLAYER!!");
        vp.url = filepath;
        vp.Play();


    }
}