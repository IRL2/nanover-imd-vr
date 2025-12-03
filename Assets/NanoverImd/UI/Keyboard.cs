using Nanover.Frontend.UI;
using Nanover.Frontend.XR;
using System;
using UnityEngine;
using UnityEngine.XR;
using Text = TMPro.TextMeshProUGUI;

namespace NanoverImd.UI
{
    public class Keyboard : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasOverride;

        [SerializeField]
        private UserInterfaceManager uiManager;

        [SerializeField]
        private GameObject rowTemplate;

        [SerializeField]
        private UiButton keyTemplate;

        [SerializeField]
        private Text currentInput;

        private Text target;
        private Action<string> onComplete;

        private string[] rows = new string[]
        {
            "1234567890-",
            "QWERTYUIOP",
            "ASDFGHJKL:",
            "ZXCVBNM./"
        };

        private void Start()
        {
            InputDeviceCharacteristics.Right.WrapUsageAsButton(CommonUsages.secondaryButton).Pressed += OnBackspace;
            InputDeviceCharacteristics.Left.WrapUsageAsButton(CommonUsages.secondaryButton).Pressed += OnBackspace;

            foreach (var row in rows) 
            {
                var rowObject = Instantiate(rowTemplate, rowTemplate.transform.parent);
                rowObject.SetActive(true);

                foreach (char letter in row)
                {
                    var letterButton = Instantiate(keyTemplate, rowObject.transform);
                    letterButton.gameObject.SetActive(true);
                    letterButton.Text = $"{letter}";
                    letterButton.OnClick += () => OnPressed(letter);
                }
            }
        }

        public void OnPressed(char letter)
        {
            currentInput.text += letter;
            target.text = currentInput.text;
        }

        public void OnBackspace()
        {
            currentInput.text = currentInput.text.Substring(0, Mathf.Max(currentInput.text.Length-1, 0));
            target.text = currentInput.text;
        }

        public void OpenForTarget(Text target)
        {
            OpenForTarget(target, null);
        }

        public void OpenForTarget(Text target, Action<string> onComplete = null)
        {
            this.target = target;
            currentInput.text = target.text;

            canvasOverride.blocksRaycasts = false;
            canvasOverride.alpha = .01f;
            gameObject.SetActive(true);

            this.onComplete = onComplete;
        }

        public void Close()
        {
            canvasOverride.blocksRaycasts = true;
            canvasOverride.alpha = 1f;
            gameObject.SetActive(false);

            onComplete?.Invoke(currentInput.text);
        }
    }
}
