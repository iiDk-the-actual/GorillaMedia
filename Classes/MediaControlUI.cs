using GorillaMedia.Classes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;

namespace GorillaMedia
{
    public class MediaControlUI : MonoBehaviour
    {
        public static MediaControlUI instance { get; private set; }
        public Canvas canvas;

        public TextMeshProUGUI title;
        public TextMeshProUGUI author;
        public TextMeshProUGUI progress;

        public Image pauseButton;
        public Image background;
        public Image icon;

        public Texture2D pauseIcon;
        public Texture2D playIcon;
        public Texture2D unknownIcon;
        public AudioClip clickSound;

        public RectTransform progressBar;
        public Material pauseMaterial;
        public Material playMaterial;

        public float maxSliderProgress;
        public Vector3 uiScale;

        public void Awake()
        {
            instance = this;

            transform.localPosition = new Vector3(0.041f, 0.111f, 0f);
            transform.localEulerAngles = new Vector3(32f, -88.55f, 92.8f);
            transform.localScale = Vector3.zero;

            uiScale = new Vector3(0.19f, 0.19f, 0.19f);

            canvas = transform.Find("Canvas").GetComponent<Canvas>();

            background = transform.Find("Canvas/Background").GetComponent<Image>();
            pauseButton = transform.Find("Canvas/Background/Pause").GetComponent<Image>();
            icon = transform.Find("Canvas/Background/Icon").GetComponent<Image>();

            title = transform.Find("Canvas/Background/Title").GetComponent<TextMeshProUGUI>();
            author = transform.Find("Canvas/Background/Author").GetComponent<TextMeshProUGUI>();
            progress = transform.Find("Canvas/Background/ElapsedTime").GetComponent<TextMeshProUGUI>();

            pauseIcon = AssetManager.LoadAsset<Texture2D>("pause");
            playIcon = AssetManager.LoadAsset<Texture2D>("play");
            unknownIcon = AssetManager.LoadAsset<Texture2D>("unknown");
            clickSound = AssetManager.LoadAsset<AudioClip>("clickSound");

            background.material = new Material(background.material)
            {
                mainTexture = AssetManager.LoadAsset<Texture2D>(
                    new string[] {
                        "BG-Spotify",
                        "BG-YouTube",
                        "BG-iTunes",
                        "BG-VLC"
                    } [ConfigManager.BackgroundIndex.Value]
                )
            };

            progressBar = transform.Find("Canvas/Background/Slider/Progress").GetComponent<RectTransform>();
            progressBar.gameObject.GetComponent<Image>().material = new Material(progressBar.gameObject.GetComponent<Image>().material)
            {
                color =
                    new Color[]
                    {
                        new Color32(29, 185, 84, 255),
                        new Color32(215, 0, 45, 255),
                        new Color32(182, 10, 204, 255),
                        new Color32(213, 97, 1, 255)
                    } [ConfigManager.BackgroundIndex.Value]
            };

            pauseButton.material = new Material(pauseButton.material);
            icon.material = new Material(icon.material);

            pauseMaterial = pauseButton.material;
            playMaterial = new Material(pauseButton.material);

            pauseMaterial.SetTexture("_MainTex", pauseIcon);
            playMaterial.SetTexture("_MainTex", playIcon);

            pauseButton.sprite = null;
            icon.sprite = null;

            maxSliderProgress = progressBar.sizeDelta.x;
            progressBar.sizeDelta = new Vector2(
                progressBar.sizeDelta.x,
                progressBar.sizeDelta.y
            );

            string[] colliderObjects = new string[]
            {
                "Previous",
                "Pause",
                "Skip"
            };
            for (int i = 0; i < colliderObjects.Length; i++)
            {
                string colliderObject = colliderObjects[i];
                transform.Find(colliderObject).gameObject.AddComponent<MediaControlButton>().buttonType = 
                    (MediaControlButton.ButtonType)i;
            }

            canvas.gameObject.SetActive(false);
        }

        public void LateUpdate()
        {
            bool rightHand = ConfigManager.HandChoice.Value == "Right";
            var activeHand = rightHand ? TrueRightHand() : TrueLeftHand();
            var handTransform = rightHand ? GorillaTagger.Instance.offlineVRRig.rightHandTransform : GorillaTagger.Instance.offlineVRRig.leftHandTransform;

            transform.position = activeHand.position;
            transform.LookAt(GorillaTagger.Instance.headCollider.transform.position);
            transform.position += transform.forward * 0.1f;
            transform.Rotate(0, 180f, 0);

            bool shouldGrow = Vector3.Distance(GorillaTagger.Instance.headCollider.transform.position, activeHand.position) < 0.7f
               && Vector3.Angle(GorillaTagger.Instance.headCollider.transform.forward, (activeHand.position - GorillaTagger.Instance.headCollider.transform.position).normalized) < 30f
               && (Vector3.Dot(
                    GorillaTagger.Instance.headCollider.transform.forward.normalized,
                    rightHand ? handTransform.right.normalized : -handTransform.right.normalized
                  ) > 0.7f);

            transform.localScale = Vector3.Lerp(transform.localScale, shouldGrow ? uiScale : Vector3.zero, Time.deltaTime * 15f);
            canvas.gameObject.SetActive(transform.localScale.magnitude > 0.005f);

            Texture2D targetIcon = MediaManager.Icon == null || !MediaManager.ValidData ? unknownIcon : MediaManager.Icon;
            if (icon.material.GetTexture("_MainTex") != targetIcon)
                icon.material.SetTexture("_MainTex", targetIcon);

            Material currentPauseMaterial = MediaManager.Paused ? playMaterial : pauseMaterial;
            if (pauseButton.material != currentPauseMaterial)
                pauseButton.material = currentPauseMaterial;

            title.text = MediaManager.Title;
            author.text = MediaManager.Artist;

            float clampedElapsed = Mathf.Clamp(MediaManager.ElapsedTime, MediaManager.StartTime, MediaManager.EndTime);
            progress.text = $"{Mathf.Floor(clampedElapsed / 60)}:{Mathf.Floor(clampedElapsed % 60):00}";
            progressBar.sizeDelta = new Vector2(
                Mathf.Lerp(0f, maxSliderProgress, (clampedElapsed - MediaManager.StartTime) / (MediaManager.EndTime - MediaManager.StartTime)),
                progressBar.sizeDelta.y
            );
        }

        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right) TrueRightHand()
        {
            Quaternion rot = GorillaTagger.Instance.rightHandTransform.rotation * GorillaLocomotion.GTPlayer.Instance.rightHandRotOffset;
            return (GorillaTagger.Instance.rightHandTransform.position + GorillaTagger.Instance.rightHandTransform.rotation * GorillaLocomotion.GTPlayer.Instance.rightHandOffset, rot, rot * Vector3.up, rot * Vector3.forward, rot * Vector3.right);
        }

        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right) TrueLeftHand()
        {
            Quaternion rot = GorillaTagger.Instance.leftHandTransform.rotation * GorillaLocomotion.GTPlayer.Instance.leftHandRotOffset;
            return (GorillaTagger.Instance.leftHandTransform.position + GorillaTagger.Instance.leftHandTransform.rotation * GorillaLocomotion.GTPlayer.Instance.leftHandOffset, rot, rot * Vector3.up, rot * Vector3.forward, rot * Vector3.right);
        }

        public class MediaControlButton : MonoBehaviour
        {
            public enum ButtonType
            {
                Previous,
                Pause,
                Skip
            }

            public void Start()
            {
                GetComponent<BoxCollider>().isTrigger = true;
                gameObject.layer = (int)UnityLayer.GorillaInteractable;
            }

            public static float buttonDelay;
            public ButtonType buttonType;
            public void OnTriggerEnter(Collider collider)
            {
                bool rightHand = ConfigManager.HandChoice.Value == "Right";
                var activeHand = rightHand ? TrueRightHand() : TrueLeftHand();

                bool shouldGrow = Vector3.Distance(GorillaTagger.Instance.headCollider.transform.position, activeHand.position) < 0.7f
                                  && Vector3.Angle(GorillaTagger.Instance.headCollider.transform.forward, (activeHand.position - GorillaTagger.Instance.headCollider.transform.position).normalized) < 30f;

                string targetCollider = rightHand ? "LeftHandTriggerCollider" : "RightHandTriggerCollider";

                if (shouldGrow && Time.time > buttonDelay && collider.name == targetCollider)
                {
                    buttonDelay = Time.time + 0.2f;

                    GorillaTagger.Instance.StartVibration(false, 
                        GorillaTagger.Instance.tagHapticStrength / 2f, 
                        GorillaTagger.Instance.tagHapticDuration / 2f);

                    AudioSource audioSource = rightHand ? GorillaTagger.Instance.offlineVRRig.rightHandPlayer : GorillaTagger.Instance.offlineVRRig.leftHandPlayer;
                    audioSource.volume = 0.3f;
                    audioSource.PlayOneShot(instance.clickSound);

                    switch (buttonType)
                    {
                        case ButtonType.Previous:
                            MediaManager.instance.PreviousTrack();
                            break;
                        case ButtonType.Pause:
                            MediaManager.instance.PauseTrack();
                            break;
                        case ButtonType.Skip:
                            MediaManager.instance.SkipTrack();
                            break;
                    }
                }
            }
        }
    }
}
