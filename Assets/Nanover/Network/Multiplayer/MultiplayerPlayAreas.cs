using Nanover.Core.Math;
using Nanover.Core.Serialization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Nanover.Network.Multiplayer
{
    /// <summary>
    /// Tracks play areas of a multiuser session i.e user-reported corners for
    /// their VR bounds in the shared space.
    /// </summary>
    public class PlayAreaCollection : MultiplayerCollection<PlayArea>
    {
        public PlayAreaCollection(MultiplayerSession session) : base(session)
        {
        }

        protected override string KeyPrefix => "playarea.";

        protected override bool ParseItem(string key, object value, out PlayArea parsed)
        {
            if (value is Dictionary<string, object> dict)
            {
                parsed = Serialization.FromDataStructure<PlayArea>(dict);
                return true;
            }

            parsed = default;
            return false;
        }

        protected override object SerializeItem(PlayArea item)
        {
            return Serialization.ToDataStructure(item);
        }
    }

    /// <summary>
    /// Tracks server-suggested transform origin for clients.
    /// </summary>
    public class PlayOriginCollection : MultiplayerCollection<PlayOrigin>
    {
        public PlayOriginCollection(MultiplayerSession session) : base(session)
        {
        }

        protected override string KeyPrefix => "user-origin.";

        protected override bool ParseItem(string key, object value, out PlayOrigin parsed)
        {
            if (value is Dictionary<string, object> dict)
            {
                parsed = Serialization.FromDataStructure<PlayOrigin>(dict);
                return true;
            }

            parsed = default;
            return false;
        }

        protected override object SerializeItem(PlayOrigin item)
        {
            return Serialization.ToDataStructure(item);
        }
    }

    /// <summary>
    /// Four corners of a VR play area.
    /// </summary>
    [DataContract]
    public class PlayArea
    {
        [DataMember]
        public Vector3 A;
        [DataMember]
        public Vector3 B;
        [DataMember]
        public Vector3 C;
        [DataMember]
        public Vector3 D;
    }

    /// <summary>
    /// A UnitScaleTransformation for user origins
    /// </summary>
    [DataContract]
    public class PlayOrigin
    {
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

        /// <summary>
        /// The component as a <see cref="UnitScaleTransformation"/>
        /// </summary>
        [IgnoreDataMember]
        public UnitScaleTransformation Transformation =>
            new UnitScaleTransformation(Position, Rotation);
    }
}
