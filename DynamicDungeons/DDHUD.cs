using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicDungeons
{
    public class DDHUD
    {
        public static DDHUD Instance;
        static GameObject dungeonName;
        static GameObject normalChestCounter;
        static GameObject specialChestCounter;
        static GameObject mobCounter;
        static GameObject cooldownTimer;
        static GameObject eventTimer;
        static GameObject alertTimer;
        static GameObject timerString;
        public static void CreateHud()
        {
            CreateDungeonName();
            CreateChestCounters();
            CreateMobCounter();
            CreateCooldownTimer();
            CreateEventTimer();
            CreateTimerString();
        }
        public static void ResetHud()
        {
            dungeonName = null;
            normalChestCounter = null;
            specialChestCounter = null;
            mobCounter = null;
            cooldownTimer = null;
            eventTimer = null;
            alertTimer = null;
            timerString = null;
        }
        public static void UpdateHud()
        {
            if (dungeonName == null) { CreateHud(); return; }
            DateTime now = new DateTime();
            TimeSpan eet = DynamicDungeons.currentDungeon.EventEnd.Subtract(now);
            TimeSpan cet = DynamicDungeons.currentDungeon.CooldownEnd.Subtract(now);
            TimeSpan aet = DynamicDungeons.currentDungeon.AlertTimerEnd.Subtract(now);
            Jotunn.Logger.LogInfo("Got Timers");
            dungeonName.GetComponent<Text>().text = (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.dungeon.name : " ");
            Jotunn.Logger.LogInfo("DDHUD Name");
            normalChestCounter.GetComponent<Text>().text = "Cofres encontrados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.FoundNormalChestCount.ToString() : " ");
            specialChestCounter.GetComponent<Text>().text = "Cofres especiales encontrados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.FoundSpecialChestCount.ToString() : " ");
            mobCounter.GetComponent<Text>().text = "Monstruos asesinados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.ActiveMobCount.ToString() : " ");
            Jotunn.Logger.LogInfo("DDHUD Counters");
            timerString.GetComponent<Text>().text = GetTimerString(now);
            cooldownTimer.GetComponent<Text>().text = DynamicDungeons.currentDungeon.EventEnd > now ? $"{cet.Hours}:{cet.Minutes}:{cet.Seconds}" : " ";
            eventTimer.GetComponent<Text>().text = DynamicDungeons.currentDungeon.CooldownEnd > now ? $"{eet.Hours}:{eet.Minutes}:{eet.Seconds}" : " ";
            alertTimer.GetComponent<Text>().text = DynamicDungeons.currentDungeon.AlertTimerEnd > now ? $"{aet.Hours}:{aet.Minutes}:{aet.Seconds}" : " ";
            Jotunn.Logger.LogInfo("DDHUD Timers");
            return;
        }
        public static void UpdateAlert()
        {
            alertTimer = null;
            CreateAlertTimer();
        }
        private static void CreateDungeonName()
        {
            dungeonName = GUIManager.Instance.CreateText(
                text: (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.dungeon.name : " "),
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(1800f, -250f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 24,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateChestCounters()
        {
            normalChestCounter = GUIManager.Instance.CreateText(
                text: "Cofres encontrados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.FoundNormalChestCount.ToString() : " "),
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(1750f, -300f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 24,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
            specialChestCounter = GUIManager.Instance.CreateText(
                text: "Cofres especiales encontrados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.FoundSpecialChestCount.ToString() : " "),
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(1750f, -350f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 24,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateMobCounter()
        {
            GUIManager.Instance.CreateText(
                text: "Monstruos asesinados: " + (DynamicDungeons.currentDungeon ? DynamicDungeons.currentDungeon.ActiveMobCount.ToString() : " "),
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(1750f, -400f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 24,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateTimerString()
        {
            timerString = GUIManager.Instance.CreateText(
                text: GetTimerString(new DateTime()),
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(0f, -100f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 24,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateEventTimer()
        {
            DateTime now = new DateTime();
            TimeSpan t = DynamicDungeons.currentDungeon.EventEnd.Subtract(now);
            cooldownTimer = GUIManager.Instance.CreateText(
                text: DynamicDungeons.currentDungeon.EventEnd > now ? $"{t.Hours}:{t.Minutes}:{t.Seconds}" : " ",
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(0f, -150f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateCooldownTimer()
        {
            DateTime now = new DateTime();
            TimeSpan t = DynamicDungeons.currentDungeon.CooldownEnd.Subtract(now);
            eventTimer = GUIManager.Instance.CreateText(
                text: DynamicDungeons.currentDungeon.CooldownEnd > now ? $"{t.Hours}:{t.Minutes}:{t.Seconds}" : " ",
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(0f, -150f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static void CreateAlertTimer()
        {
            DateTime now = new DateTime();
            TimeSpan t = DungeonManager.Instance.managers[DynamicDungeons.lastDungeon].AlertTimerEnd.Subtract(now);
            alertTimer = GUIManager.Instance.CreateText(
                text: DungeonManager.Instance.managers[DynamicDungeons.lastDungeon].AlertTimerEnd > now ? $"{t.Hours}:{t.Minutes}:{t.Seconds}" : " ",
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(0f, -100f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false
            );
        }
        private static string GetTimerString(DateTime now)
        {
            string text = "";
            if (now <= DynamicDungeons.currentDungeon.CooldownEnd) text = "COOLDOWN";
            if (now <= DynamicDungeons.currentDungeon.EventEnd) text = "TIEMPO RESTANTE";
            return text;
        }
    }
}
