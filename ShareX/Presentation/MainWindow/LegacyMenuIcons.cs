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

#nullable enable

using ShareX.AvaloniaUI.Theming;
using ShareX.HelpersLib;
using ShareX.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using DrawingBitmap = System.Drawing.Bitmap;

namespace ShareX
{
    /// <summary>
    /// The raster menu icons ShareX used before its menus moved to Avalonia.
    /// <para>
    /// Monochrome outline glyphs all tint to one colour, which makes a long menu
    /// hard to scan; these are the multi-coloured icons from ShareX 21, mapped
    /// from the Lucide glyph each entry currently asks for. Assignments are taken
    /// from ShareX 21's own MainForm designer so the menus look like they used to.
    /// </para>
    /// </summary>
    internal static class LegacyMenuIcons
    {
        private static readonly Dictionary<string, Func<DrawingBitmap>> IconsByGlyph = new()
        {
            // Capture
            [LucideIcons.maximize] = () => Resources.layer_fullscreen,
            [LucideIcons.app_window] = () => Resources.application_blue,
            [LucideIcons.monitor] = () => Resources.monitor,
            [LucideIcons.monitor_up] = () => Resources.monitor,
            [LucideIcons.scan] = () => Resources.layer_shape,
            [LucideIcons.square] = () => Resources.Rectangle,
            [LucideIcons.square_dashed] = () => Resources.layer_transparent,
            [LucideIcons.layers] = () => Resources.layers,
            [LucideIcons.scroll_text] = () => Resources.ui_scroll_pane_image,
            [LucideIcons.clock] = () => Resources.clock,
            [LucideIcons.timer] = () => Resources.clock_select,
            [LucideIcons.timer_off] = () => Resources.clock__minus,
            [LucideIcons.camera] = () => Resources.camera,
            [LucideIcons.circle_stop] = () => Resources.cross_button,
            [LucideIcons.play] = () => Resources.control,

            // Image and editing
            [LucideIcons.image] = () => Resources.image,
            [LucideIcons.image_up] = () => Resources.image_export,
            [LucideIcons.images] = () => Resources.images_stack,
            [LucideIcons.file_image] = () => Resources.image,
            [LucideIcons.wand_sparkles] = () => Resources.image_saturation,
            [LucideIcons.sparkles] = () => Resources.wand_magic,
            [LucideIcons.ruler] = () => Resources.ruler_triangle,

            // Clipboard and copying
            [LucideIcons.clipboard] = () => Resources.clipboard_paste_image,
            [LucideIcons.clipboard_list] = () => Resources.clipboard_list,
            [LucideIcons.copy] = () => Resources.document_copy,
            [LucideIcons.files] = () => Resources.document_copy,
            [LucideIcons.file] = () => Resources.document_break,
            [LucideIcons.file_text] = () => Resources.document_break,
            [LucideIcons.route] = () => Resources.clipboard_block,

            // Files on disk
            [LucideIcons.folder] = () => Resources.folder,
            [LucideIcons.folder_open] = () => Resources.folder_open_image,
            [LucideIcons.external_link] = () => Resources.folder_open_document,
            [LucideIcons.database] = () => Resources.disk,

            // Removing things
            [LucideIcons.trash_2] = () => Resources.bin,
            [LucideIcons.file_x] = () => Resources.bin,
            [LucideIcons.list_x] = () => Resources.script__minus,

            // Settings and chrome
            [LucideIcons.settings] = () => Resources.wrench_screwdriver,
            [LucideIcons.settings_2] = () => Resources.gear,
            [LucideIcons.sliders_horizontal] = () => Resources.gear,
            [LucideIcons.keyboard] = () => Resources.keyboard,
            [LucideIcons.keyboard_off] = () => Resources.keyboard__minus,
            [LucideIcons.list_checks] = () => Resources.categories,
            [LucideIcons.panel_top] = () => Resources.ui_toolbar__arrow,
            [LucideIcons.shield] = () => Resources.uac,
            [LucideIcons.bug] = () => Resources.traffic_cone,
            [LucideIcons.info] = () => Resources.crown,
            [LucideIcons.log_out] = () => Resources.cross_button,
            [LucideIcons.triangle_alert] = () => Resources.exclamation_button,
            [LucideIcons.circle_check] = () => Resources.tick_button,
            [LucideIcons.mouse_pointer_2] = () => Resources.cursor,
            [LucideIcons.link] = () => Resources.globe_share,
            [LucideIcons.cloud] = () => Resources.network_cloud,
            [LucideIcons.chevron_right] = () => Resources.navigation_000_button,
        };

        private static readonly Dictionary<string, AvaloniaBitmap?> Cache = new();

        /// <summary>
        /// The legacy icon for a glyph, or null when there is no equivalent, in
        /// which case the caller falls back to the coloured Lucide glyph.
        /// </summary>
        public static AvaloniaBitmap? ForGlyph(string glyph)
        {
            if (string.IsNullOrEmpty(glyph))
            {
                return null;
            }

            lock (Cache)
            {
                if (Cache.TryGetValue(glyph, out AvaloniaBitmap? cached))
                {
                    return cached;
                }

                AvaloniaBitmap? bitmap = null;

                if (IconsByGlyph.TryGetValue(glyph, out Func<DrawingBitmap>? factory))
                {
                    try
                    {
                        using DrawingBitmap source = factory();
                        bitmap = Convert(source);
                    }
                    catch (Exception e)
                    {
                        DebugHelper.WriteException(e);
                    }
                }

                Cache[glyph] = bitmap;
                return bitmap;
            }
        }

        private static AvaloniaBitmap Convert(DrawingBitmap source)
        {
            using MemoryStream stream = new();
            source.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return new AvaloniaBitmap(stream);
        }
    }
}
