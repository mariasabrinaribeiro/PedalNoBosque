using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Active companion ability: stop the bike, lift the puppy's ears, and sniff
    /// out nearby forest events and animals. Keyboard: F. Touch: Faro button.
    /// </summary>
    public sealed class DogFaroModeController : MonoBehaviour
    {
        private const float EventRange = 17f;
        private const float AnimalRange = 9f;
        private BikeGirlController bike;
        private InfiniteTrackGenerator track;
        private PuppyCompanionController puppy;
        private Action<string> showMessage;
        private Button modeButton;
        private TMP_Text buttonLabel;
        private float nextScanTime;
        private int lastSignalId;
        private bool lastMode;

        public void Initialize(BikeGirlController player, InfiniteTrackGenerator generator,
            PuppyCompanionController companion, Action<string> messageCallback)
        {
            bike = player;
            track = generator;
            puppy = companion;
            showMessage = messageCallback;
            CreateButton();
            SyncMode(true);
        }

        private void Update()
        {
            if (bike == null) return;
            SyncMode(false);
            if (modeButton != null && modeButton.gameObject.activeSelf)
                modeButton.gameObject.SetActive(false);
            if (!bike.isFaroMode || Time.time < nextScanTime) return;

            nextScanTime = Time.time + 0.7f;
            ScanNearby();
        }

        private void ToggleMode()
        {
            if (bike != null) bike.SetFaroMode(!bike.isFaroMode);
        }

        private void SyncMode(bool force)
        {
            bool active = bike != null && bike.isFaroMode;
            if (!force && active == lastMode) return;
            lastMode = active;
            puppy?.SetFaroMode(active);
            if (buttonLabel != null) buttonLabel.text = active ? "FARO ATIVO" : "MODO FARO  (F)";
            if (modeButton != null)
            {
                ColorBlock colors = modeButton.colors;
                colors.normalColor = active ? new Color(0.98f, 0.75f, 0.30f, 0.98f) : new Color(0.19f, 0.45f, 0.34f, 0.96f);
                colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.16f);
                colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.12f);
                modeButton.colors = colors;
            }
            if (!active) lastSignalId = 0;
        }

        private void ScanNearby()
        {
            float nearestDistance = float.MaxValue;
            int nearestId = 0;
            string message = null;
            int scentLevel = PlayerProgressManager.Instance != null
                ? PlayerProgressManager.Instance.GetSkillLevel(PlayerSkillKind.Faro) : 0;
            float eventRange = EventRange + scentLevel * 2.5f;
            float animalRange = AnimalRange + scentLevel * 2.5f;

            if (track != null)
            {
                for (int i = 0; i < track.ActiveEvents.Count; i++)
                {
                    TrailAdventureEvent ev = track.ActiveEvents[i];
                    if (ev == null || ev.isCompleted) continue;
                    float distance = Mathf.Abs(ev.transform.position.x - bike.transform.position.x);
                    if (distance > eventRange || distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestId = ev.GetInstanceID();
                    message = "O cachorrinho farejou uma descoberta: " + ev.NearbyPrompt;
                }

                for (int i = 0; i < track.ActiveAnimals.Count; i++)
                {
                    ForestAnimal animal = track.ActiveAnimals[i];
                    if (animal == null) continue;
                    float distance = Mathf.Abs(animal.transform.position.x - bike.transform.position.x);
                    if (distance > animalRange || distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestId = animal.GetInstanceID();
                    string name = string.IsNullOrWhiteSpace(animal.animalName) ? animal.kind.ToString() : animal.animalName;
                    message = "O faro percebeu um animal por perto: " + name + ". Aproxime-se devagar para observar.";
                }
            }

            if (nearestId == 0)
            {
                lastSignalId = 0;
                return;
            }
            if (nearestId == lastSignalId) return;

            lastSignalId = nearestId;
            PlayerProgressManager.Instance?.RecordSkillProgress(PlayerSkillKind.Faro);
            puppy?.TriggerCurious(2.5f);
            FlorestaSoundFX.Instance?.PlayDogBark();
            showMessage?.Invoke(message);
        }

        private void CreateButton()
        {
            GameObject canvasObject = GameObject.Find("FlorestaHUD");
            if (canvasObject == null) return;

            GameObject buttonObject = new GameObject("BtnModoFaro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-155f, 78f);
            rect.sizeDelta = new Vector2(138f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.19f, 0.45f, 0.34f, 0.96f);

            modeButton = buttonObject.GetComponent<Button>();
            modeButton.targetGraphic = image;
            modeButton.onClick.AddListener(ToggleMode);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            buttonLabel = labelObject.GetComponent<TextMeshProUGUI>();
            buttonLabel.text = "MODO FARO (F)";
            buttonLabel.font = TMP_Settings.defaultFontAsset;
            buttonLabel.fontSize = 13f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;
            buttonLabel.raycastTarget = false;
        }
    }
}
