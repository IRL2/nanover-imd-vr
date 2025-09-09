using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Nanover.Core;
using Nanover.Core.Math;
using UnityEngine;

namespace Nanover.Network.Multiplayer
{
    /// <summary>
    /// A representation of a multiplayer avatar, which is shared with other users
    /// using the shared state dictionary under the 'avatar.{playerid}' key.
    /// </summary>
    [DataContract]
    public class MultiplayerAvatar
    {
        public const string HeadsetName = "headset";
        public const string LeftHandName = "hand.left";
        public const string RightHandName = "hand.right";

        /// <summary>
        /// Player ID associated with this avatar.
        /// </summary>
        [DataMember(Name="playerid")]
        public string ID { get; set; }

        [DataMember(Name="name")]
        public string Name { get; set; }

        [DataMember(Name = "color")]
        public Color Color { get; set; }

        /// <summary>
        /// List of <see cref="AvatarComponent"/> such as headsets and controllers
        /// </summary>
        [DataMember(Name="components")]
        public List<Component> Components = new List<Component>();

        /// <summary>
        /// Set the three transformations used by this avatar.
        /// </summary>
        public void SetTransformations(Transformation? headsetTransformation,
                                       Transformation? leftHandTransformation,
                                       Transformation? rightHandTransformation)
        {
            Components.Clear();
            if (headsetTransformation.HasValue)
            {
                Components.Add(new Component(HeadsetName,
                                             headsetTransformation.Value.Position,
                                             headsetTransformation.Value.Rotation));
            }

            if (leftHandTransformation.HasValue)
            {
                Components.Add(new Component(LeftHandName,
                                             leftHandTransformation.Value.Position,
                                             leftHandTransformation.Value.Rotation));
            }

            if (rightHandTransformation.HasValue)
            {
                Components.Add(new Component(RightHandName,
                                             rightHandTransformation.Value.Position,
                                             rightHandTransformation.Value.Rotation));
            }
        }

        /// <summary>
        /// A part of an avatar, such as a headset or controller.
        /// </summary>
        [DataContract]
        public struct Component
        {
            /// <summary>
            /// The name of the component, which defines its type.
            /// </summary>
            [DataMember(Name = "name")]
            public string Name;

            /// <summary>
            /// The position of the component.
            /// </summary>
            [DataMember(Name = "position")]
            public Vector3 Position;

            /// <summary>
            /// The rotation of the component.
            /// </summary>
            [DataMember(Name = "rotation")]
            public Quaternion Rotation;
        
            public Component(string name, Vector3 position, Quaternion rotation)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
            }

            /// <summary>
            /// The component as a <see cref="UnitScaleTransformation"/>
            /// </summary>
            [IgnoreDataMember]
            public UnitScaleTransformation Transformation =>
                new UnitScaleTransformation(Position, Rotation);
        }
    }
}
