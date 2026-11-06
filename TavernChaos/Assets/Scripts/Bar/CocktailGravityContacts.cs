using System.Collections.Generic;
using UnityEngine;
namespace TavernChaos
{
    // Added only by the opt-in physics check to distinguish free fall from environmental impulses.
    public sealed class CocktailGravityContacts : MonoBehaviour
    {
        public TavernActor Owner;
        public List<string> Records;
        private void Record(Collision collision)
        {
            if(Records==null||collision.collider.GetComponentInParent<TavernActor>()==Owner)return;
            string name=collision.collider.name;if(!Records.Contains(name))Records.Add(name);
        }
        private void OnCollisionEnter(Collision collision)=>Record(collision);
        private void OnCollisionStay(Collision collision)=>Record(collision);
    }
}
