using UnityEngine;

namespace Hitbox
{
    public class TimePhysicsClock : MonoBehaviour
    {     
        private void Start()
        {
            // commit sudoku if you are not the singleton
            if(TimePhysics.Clock != this)
                Destroy(gameObject);
        }

        private void FixedUpdate() => TimePhysics.TakeSnapshot();
    }
}