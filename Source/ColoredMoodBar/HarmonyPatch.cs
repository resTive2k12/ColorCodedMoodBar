﻿using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Harmony;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MoodBarPatch {
    [StaticConstructorOnStartup]
    public class Main {
        public static MethodInfo drawSelectionOverlayOnGUIMethod;
        public static MethodInfo drawCaravanSelectionOverlayOnGUIMethod;
        public static MethodInfo getPawnTextureRectMethod;
        public static MethodInfo drawIconsMethod;
        public static FieldInfo pawnTextureCameraOffsetField;
        public static FieldInfo deadColonistTexField;
        public static FieldInfo pawnLabelsCacheField;

        public static Texture2D extremeBreakTex;
        public static Texture2D majorBreakTex;
        public static Texture2D minorBreakTex;
        public static Texture2D neutralTex;
        public static Texture2D contentTex;
        public static Texture2D happyTex;

        static Main() {
            var harmony = HarmonyInstance.Create("com.github.restive2k12.rimworld.mod.moodbar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            drawSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Pawn), typeof(Rect) }, null);
            drawCaravanSelectionOverlayOnGUIMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawCaravanSelectionOverlayOnGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(Caravan), typeof(Rect) }, null);
            getPawnTextureRectMethod = typeof(ColonistBarColonistDrawer).GetMethod("GetPawnTextureRect", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { typeof(float), typeof(float) }, null);

            drawIconsMethod = typeof(ColonistBarColonistDrawer).GetMethod("DrawIcons", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { typeof(Rect), typeof(Pawn) }, null);
            pawnTextureCameraOffsetField = typeof(ColonistBarColonistDrawer).GetField("PawnTextureCameraOffset",
                BindingFlags.Static | BindingFlags.NonPublic);
            deadColonistTexField = typeof(ColonistBarColonistDrawer).GetField("DeadColonistTex",
                    BindingFlags.Static | BindingFlags.NonPublic);
            pawnLabelsCacheField = typeof(ColonistBarColonistDrawer).GetField("pawnLabelsCache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float colorAlpha = 0.6f;
            Color red = Color.red;
            Color orange = new Color(1f, 0.5f, 0.31f, colorAlpha);
            Color yellow = Color.yellow;
            Color neutralColor = new Color(0.77f, 0.96f, 0.69f, colorAlpha);
            Color cyan = Color.cyan;
            Color happyColor = new Color(0.1f, 0.75f, 0.2f, colorAlpha);
            red.a = orange.a = yellow.a = cyan.a = colorAlpha;

            extremeBreakTex = SolidColorMaterials.NewSolidColorTexture(red);
            majorBreakTex = SolidColorMaterials.NewSolidColorTexture(orange);
            minorBreakTex = SolidColorMaterials.NewSolidColorTexture(yellow);
            neutralTex = SolidColorMaterials.NewSolidColorTexture(neutralColor);
            contentTex = SolidColorMaterials.NewSolidColorTexture(cyan);
            happyTex = SolidColorMaterials.NewSolidColorTexture(happyColor);
            LogMessage("initialized");
        }

        public static void LogMessage(string text) {
            Log.Message("[ColorCodedMoodBar] " + text);
        }
    }

    [HarmonyPatch(typeof(ColonistBarColonistDrawer))]
    [HarmonyPatch("DrawColonist")]
    [HarmonyPatch(new Type[ ] { typeof(Rect) , typeof(Pawn) , typeof(Map) , typeof(bool) , typeof(bool) } )]
    public class MoodPatch {
        private static float ApplyEntryInAnotherMapAlphaFactor(Map map, float alpha) {
            
            if (map == null) {
                if (!WorldRendererUtility.WorldRenderedNow) {
                    alpha = Mathf.Min(alpha, 0.4f);
                }
            }
            
            else if (map != Find.CurrentMap || WorldRendererUtility.WorldRenderedNow) {
                alpha = Mathf.Min(alpha, 0.4f);
            }

            return alpha;
        }

        public static bool Prefix(ColonistBarColonistDrawer __instance, ref Rect rect, ref Pawn colonist, ref Map pawnMap, ref bool highlight, ref bool reordering) {
            ColonistBar colonistBar = Find.ColonistBar;
            float alpha = ApplyEntryInAnotherMapAlphaFactor(pawnMap, colonistBar.GetEntryRectAlpha(rect));

            Rect pawnBackgroundSize = rect.ExpandedBy(2.5f);

            if (reordering) {
                alpha *= 0.5f;
            }
            Color color = new Color(1f, 1f, 1f, alpha);
            GUI.color = color;
            GUI.DrawTexture(rect, ColonistBar.BGTex);
            if (colonist.needs != null && colonist.needs.mood != null) {
                Rect position = pawnBackgroundSize.ContractedBy(2f);
                float value = position.height * colonist.needs.mood.CurLevelPercentage;
                position.yMin = position.yMax - value;
                position.height = value;


                float statValue = colonist.GetStatValue(StatDefOf.MentalBreakThreshold, true);

                float currentMoodLevel = colonist.needs.mood.CurLevel;


                // Extreme break threshold
                if (currentMoodLevel <= statValue) {
                    GUI.DrawTexture(position, Main.extremeBreakTex);
                }
                // Major break threshold
                else if (currentMoodLevel <= statValue + 0.15f) {
                    GUI.DrawTexture(position, Main.majorBreakTex);
                }
                // Minor break threshold
                else if (currentMoodLevel <= statValue + 0.3f) {
                    GUI.DrawTexture(position, Main.minorBreakTex);
                }
                // Neutral
                else if (currentMoodLevel <= 0.65f) {
                    GUI.DrawTexture(position, Main.neutralTex);
                }
                // Content
                else if (currentMoodLevel <= 0.9f) {
                    GUI.DrawTexture(position, Main.contentTex);

                }
                // Happy
                else {
                    GUI.DrawTexture(position, Main.happyTex);
                }

            }
            if (highlight) {
                int thickness = (rect.width > 22f) ? 3 : 2;
                GUI.color = Color.white;
                Widgets.DrawBox(rect, thickness);
                GUI.color = color;
            }
            Rect rect2 = rect.ContractedBy(-2f * colonistBar.Scale);
            bool notdeadandselected = (!colonist.Dead) ? Find.Selector.SelectedObjects.Contains(colonist) : Find.Selector.SelectedObjects.Contains(colonist.Corpse);
            if (notdeadandselected && !WorldRendererUtility.WorldRenderedNow) {
                Main.drawSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist, rect2 });
            } else if (WorldRendererUtility.WorldRenderedNow && colonist.IsCaravanMember() && Find.WorldSelector.IsSelected(colonist.GetCaravan())) {
                Main.drawCaravanSelectionOverlayOnGUIMethod.Invoke(__instance, new object[] { colonist.GetCaravan(), rect2 });
            }
            GUI.DrawTexture(__instance.GetPawnTextureRect(rect.position), PortraitsCache.Get(colonist, ColonistBarColonistDrawer.PawnTextureSize, ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));

            GUI.color = new Color(1f, 1f, 1f, alpha * 0.8f);
            Main.drawIconsMethod.Invoke(__instance, new object[] { rect, colonist });
            GUI.color = color;
            if (colonist.Dead) {
                GUI.DrawTexture(rect, (Texture)Main.deadColonistTexField.GetValue(__instance));
            }
            float num3 = 4f * colonistBar.Scale;
            Vector2 pos = new Vector2(rect.center.x, rect.yMax - num3);
            GenMapUI.DrawPawnLabel(colonist, pos, alpha, rect.width + colonistBar.SpaceBetweenColonistsHorizontal - 2f, (Dictionary<string, string>)Main.pawnLabelsCacheField.GetValue(__instance), GameFont.Tiny, true, true);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            return false;
        }
    }
}