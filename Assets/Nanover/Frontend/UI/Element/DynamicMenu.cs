using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanover.Frontend.UI
{
    /// <summary>
    /// A manager for a element which can display a set of options as buttons, such as a radial menu.
    /// </summary>
    public class DynamicMenu : MonoBehaviour
    {
        [SerializeField]
        private UiButton buttonPrefab;

        private List<UiButton> children = new List<UiButton>();

        public void AddItem(string name, Sprite icon, Action callback, string subtext = null)
        {
            var button = CreateButton();
            children.Add(button);
            button.Text = name;
            button.Image = icon;
            button.Subtext = subtext ?? "";
            button.OnClick += callback;
        }

        private UiButton CreateButton()
        {
            var button = Instantiate(buttonPrefab, transform);
            button.gameObject.SetActive(true);
            return button;
        }

        private void DestroyButton(UiButton button)
        {
            Destroy(button.gameObject);
        }

        public void ClearChildren()
        {
            foreach (var child in children)
                DestroyButton(child);
            children.Clear();
        }
    }
}