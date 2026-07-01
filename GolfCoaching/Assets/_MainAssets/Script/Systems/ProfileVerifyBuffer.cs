using System;
using System.Collections.Generic;
using UnityEngine;

public static class ProfileVerifyBuffer
{
    public struct RawFrame
    {
        public int width;
        public int height;
        public byte[] rgb24;
    }

    private static readonly List<RawFrame> _front = new List<RawFrame>(512);
    private static readonly List<RawFrame> _side = new List<RawFrame>(512);

    public static bool HasData => _front.Count > 0 && _side.Count == _front.Count;
    public static int Count => _front.Count;

    public static void Clear()
    {
        _front.Clear();
        _side.Clear();
    }

    public static void StoreFromTextures(List<Texture2D> front, List<Texture2D> side)
    {
        Clear();

        if (front == null || side == null || front.Count <= 0 || side.Count != front.Count)
        {
            return;
        }

        for (int i = 0; i < front.Count; i++)
        {
            Texture2D f = front[i];
            Texture2D s = side[i];

            if (f == null || s == null)
            {
                _front.Add(default);
                _side.Add(default);

                continue;
            }

            var fb = f.GetRawTextureData<byte>();
            var sb = s.GetRawTextureData<byte>();

            RawFrame rf = new RawFrame
            {
                width = f.width,
                height = f.height,
                rgb24 = fb.IsCreated ? fb.ToArray() : null
            };

            RawFrame rs = new RawFrame
            {
                width = s.width,
                height = s.height,
                rgb24 = sb.IsCreated ? sb.ToArray() : null
            };

            _front.Add(rf);
            _side.Add(rs);
        }

        //Debug.Log($"[ProfileVerifyBuffer] Stored: {_front.Count} frames");
    }

    public static void Export(out List<RawFrame> front, out List<RawFrame> side)
    {
        front = new List<RawFrame>(_front.Count);
        side = new List<RawFrame>(_side.Count);

        for (int i = 0; i < _front.Count; i++)
        {
            RawFrame f = _front[i];
            RawFrame s = _side[i];

            RawFrame nf = new RawFrame
            {
                width = f.width,
                height = f.height,
                rgb24 = (f.rgb24 != null) ? (byte[])f.rgb24.Clone() : null
            };

            RawFrame ns = new RawFrame
            {
                width = s.width,
                height = s.height,
                rgb24 = (s.rgb24 != null) ? (byte[])s.rgb24.Clone() : null
            };

            front.Add(nf);
            side.Add(ns);
        }
    }

    public static void ExportAsTextures(out List<Texture2D> frontTextures, out List<Texture2D> sideTextures)
    {
        frontTextures = new List<Texture2D>(_front.Count);
        sideTextures = new List<Texture2D>(_side.Count);

        for (int i = 0; i < _front.Count; i++)
        {
            RawFrame f = _front[i];
            RawFrame s = _side[i];

            Texture2D ft = null;
            Texture2D st = null;

            if (f.rgb24 != null && f.width > 0 && f.height > 0)
            {
                ft = new Texture2D(f.width, f.height, TextureFormat.RGB24, false);
                ft.LoadRawTextureData(f.rgb24);
                ft.Apply(false, false);
            }

            if (s.rgb24 != null && s.width > 0 && s.height > 0)
            {
                st = new Texture2D(s.width, s.height, TextureFormat.RGB24, false);
                st.LoadRawTextureData(s.rgb24);
                st.Apply(false, false);
            }

            frontTextures.Add(ft);
            sideTextures.Add(st);
        }
    }
}
