#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShareX.ScreenCaptureLib
{
    public class ArrowDrawingShape : LineDrawingShape
    {
        public override ShapeType ShapeType { get; } = ShapeType.DrawingArrow;

        public ArrowHeadDirection ArrowHeadDirection { get; set; }
        public bool ShowLength { get; set; }

        public override void OnConfigLoad()
        {
            base.OnConfigLoad();
            ArrowHeadDirection = AnnotationOptions.ArrowHeadDirection;
            ShowLength = AnnotationOptions.ArrowShowLength;
        }

        public override void OnConfigSave()
        {
            base.OnConfigSave();
            AnnotationOptions.ArrowHeadDirection = ArrowHeadDirection;
            AnnotationOptions.ArrowShowLength = ShowLength;
        }

        public override void OnDraw(Graphics g)
        {
            base.OnDraw(g);

            if (ShowLength)
            {
                DrawLength(g);
            }
        }

        // Straight chord distance between the endpoints, not the on-screen path length
        // (curved/center-point arrows can visually bow away from this value).
        private void DrawLength(Graphics g)
        {
            PointF start = Points[0];
            PointF end = Points[Points.Length - 1];
            float distance = MathHelpers.Distance(start, end);

            if (distance < 1)
            {
                return;
            }

            string text = $"{distance:0} px";
            float fontSize = Math.Max(8, Math.Min(16, (BorderSize * 2) + 8));

            using (Font font = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, font);

                PointF mid = new PointF((start.X + end.X) / 2f, (start.Y + end.Y) / 2f);
                float lineAngle = MathHelpers.LookAtDegree(start, end);

                // Keep the label upright regardless of arrow direction.
                float textAngle = lineAngle;
                if (textAngle > 90 || textAngle < -90)
                {
                    textAngle -= 180;
                }

                // Offset perpendicular to the line so the label doesn't sit on top of the shaft.
                float offset = (textSize.Height / 2f) + Math.Max(BorderSize, 2) + 4;
                double perpendicularRadian = (lineAngle - 90) * Math.PI / 180.0;
                PointF textCenter = new PointF(
                    mid.X + (float)(Math.Cos(perpendicularRadian) * offset),
                    mid.Y + (float)(Math.Sin(perpendicularRadian) * offset));

                RectangleF textRect = new RectangleF(-textSize.Width / 2f, -textSize.Height / 2f, textSize.Width, textSize.Height);

                GraphicsState state = g.Save();
                g.TranslateTransform(textCenter.X, textCenter.Y);
                g.RotateTransform(textAngle);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    if (Shadow)
                    {
                        using (Brush shadowBrush = new SolidBrush(ShadowColor))
                        {
                            g.DrawString(text, font, shadowBrush, textRect.LocationOffset(ShadowOffset), sf);
                        }
                    }

                    using (Brush textBrush = new SolidBrush(BorderColor))
                    {
                        g.DrawString(text, font, textBrush, textRect, sf);
                    }
                }

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                g.SmoothingMode = SmoothingMode.None;
                g.Restore(state);
            }
        }

        protected override Pen CreatePen(Color borderColor, int borderSize, BorderStyle borderStyle)
        {
            using (GraphicsPath gp = new GraphicsPath())
            {
                int arrowWidth = 2, arrowHeight = 6, arrowCurve = 1;
                gp.AddLine(new Point(0, 0), new Point(-arrowWidth, -arrowHeight));
                gp.AddCurve(new Point[] { new Point(-arrowWidth, -arrowHeight), new Point(0, -arrowHeight + arrowCurve), new Point(arrowWidth, -arrowHeight) });
                gp.CloseFigure();

                CustomLineCap lineCap = new CustomLineCap(gp, null)
                {
                    BaseInset = arrowHeight - arrowCurve
                };

                Pen pen = new Pen(borderColor, borderSize);

                if (ArrowHeadDirection == ArrowHeadDirection.Both && MathHelpers.Distance(Points[0], Points[Points.Length - 1]) > arrowHeight * borderSize * 2)
                {
                    pen.CustomEndCap = pen.CustomStartCap = lineCap;
                }
                else if (ArrowHeadDirection == ArrowHeadDirection.Start)
                {
                    pen.CustomStartCap = lineCap;
                }
                else
                {
                    pen.CustomEndCap = lineCap;
                }

                pen.LineJoin = LineJoin.Round;
                pen.DashStyle = (DashStyle)borderStyle;
                return pen;
            }
        }
    }
}
