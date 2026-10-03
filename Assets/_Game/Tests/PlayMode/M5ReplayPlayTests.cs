#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Starfall.Tests
{
    public sealed partial class CampaignPlayTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator M5FirstStageReplayUsesNormalInputAndCapturesActualRendering()
        {
            game.SetLanguage("zh-CN");
            Assert.IsTrue(game.StartStage(1));
            yield return null;
            var recorder = game.gameObject.AddComponent<M5ReplayCapture>();
            recorder.Begin(game);
            yield return WalkTo(new Vector2(-5.7f, -4));
            foreach (string room in new[] { "courtyard", "north", "crossing", "south" })
            {
                Assert.AreEqual(room, game.Adventure.Current.Id);
                yield return FightUsingInput();
                if (room == "courtyard")
                {
                    yield return WalkTo(new Vector2(-5, -3));
                    yield return Press(Key.F);
                    yield return WalkTo(new Vector2(-4, -3));
                    yield return Press(Key.E);
                    Assert.AreEqual("shotgun", game.Loadout.SpecialWeapon);
                    yield return Press(Key.Digit2);
                    yield return new WaitForSecondsRealtime(.8f);
                }
                if (room == "north" || room == "south")
                {
                    yield return WalkTo(new Vector2(7, 2));
                    yield return Press(Key.F);
                }
                if (room == "south")
                {
                    yield return WalkTo(new Vector2(-6, -4));
                    yield return Press(Key.F);
                }
                yield return WalkTo(game.Room.Exit);
                yield return Press(Key.F);
            }
            yield return FightUsingInput();
            Assert.IsTrue(game.Adventure.Result.Success);
            Assert.IsTrue(game.Adventure.Result.Eligible);
            Assert.IsTrue(game.Adventure.Result.Saved);
            yield return new WaitForSecondsRealtime(1.5f);
            recorder.Finish();
            Assert.Greater(recorder.FrameCount, 120);
        }
    }

    // Evidence capture belongs only to the editor test assembly, never to the player.
    public sealed class M5ReplayCapture : MonoBehaviour
    {
        StarfallGame game;
        string directory;
        float started, next;
        RenderTexture target;
        Texture2D image;
        readonly List<string> index = new List<string>();
        public int FrameCount => index.Count;

        public void Begin(StarfallGame owner)
        {
            game = owner;
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../appendix/M5/replay/frames"));
            Directory.CreateDirectory(directory);
            target = new RenderTexture(1280, 720, 24);
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            started = Time.realtimeSinceStartup;
            next = started;
        }

        void LateUpdate()
        {
            if (game == null || Time.realtimeSinceStartup < next) return;
            float stamp = Time.realtimeSinceStartup - started;
            next = Time.realtimeSinceStartup + 1f / 12f;
            var camera = game.GameCamera;
            var canvas = game.Interface.Canvas;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var oldMode = canvas.renderMode;
            var oldWorldCamera = canvas.worldCamera;
            float oldScale = canvas.scaleFactor, oldSize = camera.orthographicSize, oldPlane = canvas.planeDistance;
            bool oldEnabled = scaler.enabled;
            try
            {
                camera.targetTexture = target;
                camera.orthographicSize = 9.3f;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5;
                scaler.enabled = false;
                canvas.scaleFactor = 1;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string name = "frame-" + index.Count.ToString("D5") + ".jpg";
                File.WriteAllBytes(Path.Combine(directory, name), image.EncodeToJPG(85));
                index.Add(stamp.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "|" + name);
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.orthographicSize = oldSize;
                RenderTexture.active = oldActive;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldWorldCamera;
                canvas.planeDistance = oldPlane;
                canvas.scaleFactor = oldScale;
                scaler.enabled = oldEnabled;
                Canvas.ForceUpdateCanvases();
            }
        }

        public void Finish()
        {
            game = null;
            File.WriteAllLines(Path.Combine(directory, "../frames.txt"), index);
        }

        void OnDestroy()
        {
            if (game != null) Finish();
            if (target != null) { target.Release(); Destroy(target); }
            if (image != null) Destroy(image);
        }
    }
}
#endif
