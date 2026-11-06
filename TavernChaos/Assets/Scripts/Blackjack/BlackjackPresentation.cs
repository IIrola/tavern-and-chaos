using UnityEngine;

namespace TavernChaos
{
    // Provisional table dressing. Cards are drawn by the HUD from public data; nothing here decides play.
    public static class BlackjackPresentation
    {
        public static void BuildTable()
        {
            var root = new GameObject("Blackjack | provisional table").transform; root.position = BlackjackRoomController.Center;
            var felt = TavernWorld.Mat("Blackjack felt", new Color(.05f, .33f, .22f));
            var top = TavernWorld.Cylinder("Table top", new(0, 1.0f, 0), new(2.9f, .09f, 2.9f), TavernWorld.Wood, root);
            TavernWorld.Cylinder("Table felt", new(0, 1.09f, 0), new(2.7f, .04f, 2.7f), felt, root, false);
            TavernWorld.Cylinder("Table stem", new(0, .48f, 0), new(.4f, .48f, .4f), TavernWorld.DarkWood, root);
            TavernWorld.Cylinder("Dealer shoe", new(.85f, 1.17f, 0), new(.3f, .1f, .3f), TavernWorld.Black, root, false);
            TavernWorld.Text("04 / BLACKJACK", new(.2f, 1.13f, 0), .16f, new Color(.95f, .85f, .5f), root);
            root.GetChild(root.childCount - 1).localRotation = Quaternion.Euler(90, 90, 0);
            for (int i = 0; i < 5; i++) {
                var local = BlackjackRoomController.SeatPosition(i) - BlackjackRoomController.Center;
                TavernWorld.Cylinder("Seat visual", new(local.x, .75f, local.z), new(.5f, .06f, .5f), TavernWorld.DarkWood, root, false);
                TavernWorld.Cylinder("Seat stem", new(local.x, .38f, local.z), new(.08f, .38f, .08f), TavernWorld.Metal, root, false);
                var bet = (Vector3)local * .55f; TavernWorld.Cylinder("Bet spot", new(bet.x, 1.115f, bet.z), new(.34f, .004f, .34f), TavernWorld.Cream, root, false);
            }
            TavernWorld.LightAt(BlackjackRoomController.Center + Vector3.up * 3.2f, new Color(1f, .8f, .5f), 2.2f, 6);
        }
    }
}
