using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nanover.Frontend.Controllers;

using Nanover.Frontend.UI;
using Nanover.Frontend.XR;
using UnityEngine;
using UnityEngine.XR;

namespace NanoverImd.UI
{
    public class UserInterfaceManager : MonoBehaviour
    {
        private GameObject currentScenePrefab;
        
        [SerializeField]
        private GameObject currentScene;

        [SerializeField]
        private GameObject initialScene;

        [SerializeField]
        private GameObject sceneUI;

        [SerializeField]
        public GameObject simulation;

        private Stack<GameObject> sceneStack = new Stack<GameObject>();

        [SerializeField]
        public InputDeviceCharacteristics characteristics;

        public bool SimulationActive => simulation.activeInHierarchy;
        public bool SimulationMenuActive => sceneUI.activeInHierarchy;

        public GameObject SceneUI => sceneUI;

        private void Start()
        {
            if (initialScene != null)
                GotoScene(initialScene);

            SetupOutOfSimulationMenu();
        }

        private void SetupOutOfSimulationMenu()
        {
            // hides any open full-screen ui (like options or change-sim menu)
            var menuButton = characteristics.WrapUsageAsButton(CommonUsages.menuButton, () => SimulationActive && SimulationMenuActive);
            menuButton.Pressed += CloseScene;
        }

        private void LeaveScene(GameObject scene)
        {
            WorldSpaceCursorInput.ClearSelection();
            Destroy(scene);
        }

        private GameObject EnterScene(GameObject scene)
        {
            if (scene != null)
            {
                var newScene = Instantiate(scene, sceneUI.transform);
                newScene.SetActive(true);
                return newScene;
            }

            return null;
        }

        public void GotoScene(GameObject scene)
        {
            if (currentScene != null)
                LeaveScene(currentScene);
            currentScene = EnterScene(scene);
            if (currentScene != null)
                currentScenePrefab = scene;
            else
                currentScenePrefab = null;
            sceneUI.SetActive(currentScene != null);
        }

        public void GotoSceneAndAddToStack(GameObject newScene)
        {
            var previousScenePrefab = currentScenePrefab;
            GotoScene(newScene);
            if (newScene != null && previousScenePrefab != null)
                sceneStack.Push(previousScenePrefab);
        }

        public void GoBack()
        {
            if (sceneStack.Any())
            {
                GotoScene(sceneStack.Pop());
                sceneStack.Clear();
            }
        }

        public void CloseScene()
        {
            sceneStack.Clear();
            GotoScene(null);
        }
    }
}