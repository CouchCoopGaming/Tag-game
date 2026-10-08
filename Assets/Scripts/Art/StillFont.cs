using System;
using System.Collections.Generic;
using System.IO;

namespace Tag.Art
{
    /// <summary>
    /// Rasterizes still labels from the bundled Liberation Sans. The old 5-by-7
    /// bitmap only had some capitals, so JUMP, ROPE, ZIP, PAD, and REV lost
    /// letters. Nothing here runs on the gameplay hot path.
    /// </summary>
    public static class StillFont
    {
        const string FileName = "LiberationSans-Regular.ttf";

        static byte[] _font;
        static bool _ready;
        static int _units = 2048;
        static int _ascender = 1854;
        static int _descender = -434;
        static int _loca;
        static int _glyf;
        static int _hmtx;
        static int _numH = 1;
        static int _cmap;
        static int _cmapEnd;
        static int _segCount;
        static int _endOff;
        static int _startOff;
        static int _deltaOff;
        static int _rangeOff;

        public static bool Ready()
        {
            if (_ready) return _font != null;
            _ready = true;
            string path = Find();
            if (path == null) return false;
            try
            {
                _font = File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                _font = null;
                return false;
            }
            return Parse();
        }

        /// <summary>Draws left to right. y is the top of the line. Returns the width used.</summary>
        public static int Draw(byte[] pix, int w, int h, int x, int y, string text, byte r, byte g, byte b, int pixelHeight)
        {
            if (string.IsNullOrEmpty(text) || pix == null) return 0;
            if (pixelHeight < 8) pixelHeight = 8;
            if (!Ready()) return 0;
            float scale = pixelHeight / (float)(_ascender - _descender);
            int baseline = y + (int)(_ascender * scale + 0.5f);
            int pen = x;
            for (int i = 0; i < text.Length; i++)
            {
                int cp = text[i];
                if (cp == ' ')
                {
                    pen += Advance(3, scale);
                    continue;
                }
                int gid = GlyphId(cp);
                if (gid <= 0)
                {
                    pen += (int)(pixelHeight * 0.5f);
                    continue;
                }
                pen += Raster(pix, w, h, pen, baseline, gid, scale, r, g, b);
            }
            return pen - x;
        }

        static int Raster(byte[] pix, int w, int h, int pen, int baseline, int gid, float scale, byte r, byte g, byte b)
        {
            int adv = Advance(gid, scale);
            int goff = GlyphOffset(gid);
            int g2 = GlyphOffset(gid + 1);
            if (goff < 0 || g2 <= goff) return adv;
            int nc = S16(goff);
            if (nc <= 0) return adv;
            var contours = new List<float[]>(nc);
            if (!ReadContours(goff, nc, contours)) return adv;
            var edges = new List<float>(256);
            for (int c = 0; c < contours.Count; c++)
                Flatten(contours[c], edges);
            if (edges.Count < 4) return adv;
            float minY = edges[1];
            float maxY = edges[1];
            for (int i = 1; i < edges.Count; i += 2)
            {
                if (edges[i] < minY) minY = edges[i];
                if (edges[i] > maxY) maxY = edges[i];
            }
            int y0 = baseline - (int)Math.Ceiling(maxY * scale);
            int y1 = baseline - (int)Math.Floor(minY * scale);
            if (y0 < 0) y0 = 0;
            if (y1 >= h) y1 = h - 1;
            var hits = new List<float>(32);
            for (int py = y0; py <= y1; py++)
            {
                float fy = (baseline - (py + 0.5f)) / scale;
                hits.Clear();
                for (int i = 0; i + 3 < edges.Count; i += 4)
                {
                    float x0 = edges[i];
                    float yA = edges[i + 1];
                    float x1 = edges[i + 2];
                    float yB = edges[i + 3];
                    if (yA == yB) continue;
                    bool down = yA > yB;
                    float yTop = down ? yA : yB;
                    float yBot = down ? yB : yA;
                    if (fy >= yTop || fy < yBot) continue;
                    float t = (fy - yA) / (yB - yA);
                    hits.Add(x0 + (x1 - x0) * t);
                }
                if (hits.Count < 2) continue;
                hits.Sort();
                for (int i = 0; i + 1 < hits.Count; i += 2)
                {
                    int xa = pen + (int)Math.Floor(hits[i] * scale);
                    int xb = pen + (int)Math.Ceiling(hits[i + 1] * scale);
                    if (xa < 0) xa = 0;
                    if (xb >= w) xb = w - 1;
                    for (int px = xa; px <= xb; px++)
                        Plot(pix, w, h, px, py, r, g, b);
                }
            }
            return adv;
        }

        static void Flatten(float[] pts, List<float> edges)
        {
            int n = pts.Length / 3;
            if (n < 2) return;
            int start = 0;
            float sx;
            float sy;
            if (pts[2] > 0.5f)
            {
                sx = pts[0];
                sy = pts[1];
            }
            else if (pts[(n - 1) * 3 + 2] > 0.5f)
            {
                start = n - 1;
                sx = pts[start * 3];
                sy = pts[start * 3 + 1];
            }
            else
            {
                sx = (pts[(n - 1) * 3] + pts[0]) * 0.5f;
                sy = (pts[(n - 1) * 3 + 1] + pts[1]) * 0.5f;
            }
            float cx = sx;
            float cy = sy;
            for (int k = 1; k <= n; k++)
            {
                int i = (start + k) % n;
                float x = pts[i * 3];
                float y = pts[i * 3 + 1];
                bool on = pts[i * 3 + 2] > 0.5f;
                if (on)
                {
                    Line(edges, cx, cy, x, y);
                    cx = x;
                    cy = y;
                    continue;
                }
                int j = (i + 1) % n;
                float nx = pts[j * 3];
                float ny = pts[j * 3 + 1];
                bool nextOn = pts[j * 3 + 2] > 0.5f;
                float ex = nextOn ? nx : (x + nx) * 0.5f;
                float ey = nextOn ? ny : (y + ny) * 0.5f;
                Quad(edges, cx, cy, x, y, ex, ey, 0);
                cx = ex;
                cy = ey;
                if (nextOn) k++;
            }
            if (cx != sx || cy != sy)
                Line(edges, cx, cy, sx, sy);
        }

        static void Quad(List<float> edges, float x0, float y0, float x1, float y1, float x2, float y2, int depth)
        {
            float dx = x2 - x0;
            float dy = y2 - y0;
            float flat = (x1 - x2) * dy - (y1 - y2) * dx;
            if (flat < 0f) flat = -flat;
            if (depth >= 6 || flat <= 12f)
            {
                Line(edges, x0, y0, x2, y2);
                return;
            }
            float ax = (x0 + x1) * 0.5f;
            float ay = (y0 + y1) * 0.5f;
            float bx = (x1 + x2) * 0.5f;
            float by = (y1 + y2) * 0.5f;
            float mx = (ax + bx) * 0.5f;
            float my = (ay + by) * 0.5f;
            Quad(edges, x0, y0, ax, ay, mx, my, depth + 1);
            Quad(edges, mx, my, bx, by, x2, y2, depth + 1);
        }

        static void Line(List<float> edges, float x0, float y0, float x1, float y1)
        {
            edges.Add(x0);
            edges.Add(y0);
            edges.Add(x1);
            edges.Add(y1);
        }

        static bool ReadContours(int g, int nc, List<float[]> contours)
        {
            int p = g + 10;
            var ends = new int[nc];
            for (int i = 0; i < nc; i++)
            {
                ends[i] = U16(p);
                p += 2;
            }
            int npts = ends[nc - 1] + 1;
            if (npts <= 0 || npts > 2000) return false;
            int instr = U16(p);
            p += 2 + instr;
            if (p + npts > _font.Length) return false;
            var flags = new byte[npts];
            for (int i = 0; i < npts;)
            {
                byte f = _font[p++];
                flags[i++] = f;
                if ((f & 8) != 0)
                {
                    int rep = _font[p++];
                    for (int k = 0; k < rep && i < npts; k++)
                        flags[i++] = f;
                }
            }
            var xs = new int[npts];
            var ys = new int[npts];
            int x = 0;
            for (int i = 0; i < npts; i++)
            {
                byte f = flags[i];
                if ((f & 2) != 0)
                {
                    int dx = _font[p++];
                    x += (f & 16) != 0 ? dx : -dx;
                }
                else if ((f & 16) == 0)
                {
                    x += S16(p);
                    p += 2;
                }
                xs[i] = x;
            }
            int y = 0;
            for (int i = 0; i < npts; i++)
            {
                byte f = flags[i];
                if ((f & 4) != 0)
                {
                    int dy = _font[p++];
                    y += (f & 32) != 0 ? dy : -dy;
                }
                else if ((f & 32) == 0)
                {
                    y += S16(p);
                    p += 2;
                }
                ys[i] = y;
            }
            int from = 0;
            for (int c = 0; c < nc; c++)
            {
                int to = ends[c];
                int count = to - from + 1;
                var pts = new float[count * 3];
                for (int i = 0; i < count; i++)
                {
                    pts[i * 3] = xs[from + i];
                    pts[i * 3 + 1] = ys[from + i];
                    pts[i * 3 + 2] = (flags[from + i] & 1) != 0 ? 1f : 0f;
                }
                contours.Add(pts);
                from = to + 1;
            }
            return true;
        }

        static int Advance(int gid, float scale)
        {
            int adv;
            if (gid < _numH)
                adv = U16(_hmtx + gid * 4);
            else
                adv = U16(_hmtx + (_numH - 1) * 4);
            int px = (int)(adv * scale + 0.5f);
            if (px < 1) px = 1;
            return px + 1;
        }

        static int GlyphOffset(int gid)
        {
            if (gid < 0) return -1;
            return _glyf + (int)U32(_loca + gid * 4);
        }

        static int GlyphId(int cp)
        {
            if (_segCount <= 0) return 0;
            for (int i = 0; i < _segCount; i++)
            {
                int end = U16(_endOff + i * 2);
                if (end < cp) continue;
                int start = U16(_startOff + i * 2);
                if (cp < start) return 0;
                int delta = S16(_deltaOff + i * 2);
                int ro = U16(_rangeOff + i * 2);
                if (ro == 0)
                    return (cp + delta) & 0xFFFF;
                int glyphOff = _rangeOff + i * 2 + ro + (cp - start) * 2;
                int g = U16(glyphOff);
                if (g == 0) return 0;
                return (g + delta) & 0xFFFF;
            }
            return 0;
        }

        static bool Parse()
        {
            if (_font == null || _font.Length < 12) return false;
            int num = U16(4);
            int off = 12;
            int cmap = 0;
            int head = 0;
            int maxp = 0;
            int hhea = 0;
            for (int i = 0; i < num; i++)
            {
                if (off + 16 > _font.Length) return false;
                string tag = TagAt(off);
                int ofs = (int)U32(off + 8);
                if (tag == "cmap") cmap = ofs;
                else if (tag == "head") head = ofs;
                else if (tag == "loca") _loca = ofs;
                else if (tag == "glyf") _glyf = ofs;
                else if (tag == "hmtx") _hmtx = ofs;
                else if (tag == "maxp") maxp = ofs;
                else if (tag == "hhea") hhea = ofs;
                off += 16;
            }
            if (cmap == 0 || head == 0 || _loca == 0 || _glyf == 0 || _hmtx == 0) return false;
            _units = U16(head + 18);
            if (_units < 16) _units = 2048;
            if (hhea != 0)
            {
                _ascender = S16(hhea + 4);
                _descender = S16(hhea + 6);
                _numH = U16(hhea + 34);
            }
            if (_numH < 1) _numH = 1;
            if (maxp != 0 && U16(maxp + 4) <= 0) return false;
            int nsub = U16(cmap + 2);
            int chosen = 0;
            int p = cmap + 4;
            for (int i = 0; i < nsub; i++)
            {
                int plat = U16(p);
                int enc = U16(p + 2);
                int so = (int)U32(p + 4);
                int fmt = U16(cmap + so);
                if (fmt == 4 && (plat == 3 || plat == 0))
                {
                    chosen = cmap + so;
                    break;
                }
                if (chosen == 0 && fmt == 4)
                    chosen = cmap + so;
                p += 8;
            }
            if (chosen == 0) return false;
            _cmap = chosen;
            _segCount = U16(chosen + 6) / 2;
            if (_segCount <= 0 || _segCount > 400) return false;
            _endOff = chosen + 14;
            _startOff = _endOff + _segCount * 2 + 2;
            _deltaOff = _startOff + _segCount * 2;
            _rangeOff = _deltaOff + _segCount * 2;
            _cmapEnd = chosen + U16(chosen + 2);
            return GlyphId('A') > 0 && GlyphId('J') > 0 && GlyphId('P') > 0;
        }

        static string Find()
        {
            string env = Environment.GetEnvironmentVariable("TAG_STILL_FONT");
            if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
            var dirs = new List<string>();
            dirs.Add(Directory.GetCurrentDirectory());
            try
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc))
                    dirs.Add(Path.GetDirectoryName(loc));
            }
            catch (NotSupportedException)
            {
            }
            string[] rel =
            {
                Path.Combine("Tools", "StrafeJumpSim", "Fonts", FileName),
                Path.Combine("Fonts", FileName),
            };
            for (int d = 0; d < dirs.Count; d++)
            {
                string dir = dirs[d];
                for (int up = 0; up < 8 && !string.IsNullOrEmpty(dir); up++)
                {
                    for (int r = 0; r < rel.Length; r++)
                    {
                        string path = Path.Combine(dir, rel[r]);
                        if (File.Exists(path)) return path;
                    }
                    DirectoryInfo parent = Directory.GetParent(dir);
                    dir = parent == null ? null : parent.FullName;
                }
            }
            string system = "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf";
            if (File.Exists(system)) return system;
            return null;
        }

        static string TagAt(int o)
        {
            char a = (char)_font[o];
            char b = (char)_font[o + 1];
            char c = (char)_font[o + 2];
            char d = (char)_font[o + 3];
            return new string(new[] { a, b, c, d });
        }

        static int U16(int o)
        {
            return (_font[o] << 8) | _font[o + 1];
        }

        static int S16(int o)
        {
            int v = U16(o);
            if (v >= 32768) v -= 65536;
            return v;
        }

        static uint U32(int o)
        {
            return (uint)((_font[o] << 24) | (_font[o + 1] << 16) | (_font[o + 2] << 8) | _font[o + 3]);
        }

        static void Plot(byte[] pix, int w, int h, int x, int y, byte r, byte g, byte b)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int p = (y * w + x) * 3;
            pix[p] = r;
            pix[p + 1] = g;
            pix[p + 2] = b;
        }
    }
}
