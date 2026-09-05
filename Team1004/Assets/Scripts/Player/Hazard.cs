using UnityEngine;

namespace Game.Player
{
    public class Hazard : MonoThing
    {
        [SerializeField] private string kind = "Rock";

        public string Kind => kind;
    }
}
