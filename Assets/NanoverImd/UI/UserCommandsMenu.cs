using System.Collections.Generic;
using System.Linq;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.UI;
using UnityEngine;

namespace NanoverImd.UI
{
    public class UserCommandsMenu : MonoBehaviour
    {
        [SerializeField]
        private VrController notifiedController;

        [SerializeField]
        private Sprite commandIcon;

        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private DynamicMenu menu;

        private void OnEnable()
        {
            RefreshCommands();
        }

        public async void RefreshCommands()
        {
            await simulation.Trajectory.UpdateCommands();
            RefreshButtons();
        }
        
        public void RefreshButtons()
        {
            menu.ClearChildren();

            foreach (var command in simulation.Trajectory.CommandDefinitions.Values.Where(command => command.Name.StartsWith("user/")))
            {
                async void RunCommand()
                {
                    notifiedController.PushNotification($"Run {command.Name}");
                    var result = await simulation.Trajectory.RunCommand(command.Name);
                    if (result != null && result.TryGetValue("result", out object notification))
                        notifiedController.PushNotification($"{command.Name}: {notification}");
                    else if (result != null)
                        notifiedController.PushNotification($"Completed {command.Name}");
                }

                menu.AddItem(command.Name, commandIcon, RunCommand);
            }
        }
    }
}