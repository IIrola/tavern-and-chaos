using UnityEngine;
namespace TavernChaos
{
    public enum TavernActivity { Pool, Blackjack, Poker, Domino, GoFish, ArmWrestle }
    [CreateAssetMenu(fileName="EconomySettings",menuName="Tavern/Session economy")]
    public sealed class EconomySettings : ScriptableObject
    {
        [SerializeField,Min(0)] private int startingChips=1000;
        [SerializeField,Min(1)] private int pool=10,blackjack=10,poker=10,domino=5,goFish=5,armWrestle=5,drink=20;
        [SerializeField,Min(2)] private int blackjackMinimum=26;
        public long StartingChips=>startingChips;
        public long Drink=>drink;
        public long BlackjackMinimum=>blackjackMinimum;
        public long Opening(TavernActivity kind)=>kind switch { TavernActivity.Pool=>pool,TavernActivity.Blackjack=>blackjack,TavernActivity.Poker=>poker,TavernActivity.Domino=>domino,TavernActivity.GoFish=>goFish,_=>armWrestle };
        public bool Valid=>startingChips>=0&&pool>0&&blackjack>0&&poker>0&&domino>0&&goFish>0&&armWrestle>0&&drink>0&&blackjackMinimum>=2&&blackjackMinimum%2==0;
    }
}
