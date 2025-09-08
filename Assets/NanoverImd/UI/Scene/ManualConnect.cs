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
        private TMP_Text hostInputField;
        [SerializeField]
        private TMP_Text portInput;
    
        private void Start()
        {
            Assert.IsNotNull(application);
            Assert.IsNotNull(hostInputField);
            Assert.IsNotNull(portInput);
        }

        /// <summary>
        /// Called from the UI button to tell the application to connect
        /// to remote services.
        /// </summary>
        public void ConnectToServer()
        {
            var port = portInput.text.Length > 0
                     ? (int?) int.Parse(portInput.text)
                     : null;

            application.Simulation.ConnectWebSocket($"{hostInputField.text}:{port}").Forget();
        }
    }
}
