using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace NanoverImd.UI.Scene
{
    public class ManualConnect : MonoBehaviour
    {
        [SerializeField]
        private NanoverImdApplication application;
        [SerializeField]
        private TMP_Text addressInputField;

        private void Start()
        {
            Assert.IsNotNull(application);
            Assert.IsNotNull(addressInputField);
        }

        /// <summary>
        /// Called from the UI button to tell the application to connect
        /// to remote services.
        /// </summary>
        public void ConnectToServer()
        {
            application.Simulation.ConnectWebSocket(addressInputField.text).Forget();
        }
    }
}
