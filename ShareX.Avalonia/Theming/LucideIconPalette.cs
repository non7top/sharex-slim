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

using Avalonia.Media;
using System.Collections.Generic;

namespace ShareX.AvaloniaUI.Theming;

/// <summary>
/// Semantic colours for the Lucide menu icons.
/// <para>
/// Every menu icon used to be painted in the single accent colour, which made
/// a menu read as a column of identical grey-blue glyphs. Grouping them by what
/// they do restores the at-a-glance scannability the old raster icon set had.
/// Hues are mid-tone on purpose so they stay legible on both the dark and the
/// light theme.
/// </para>
/// </summary>
public static class LucideIconPalette
{
    private static readonly Color Capture = Color.Parse("#3E83F2");     // blue
    private static readonly Color Editing = Color.Parse("#8B5CF6");     // violet
    private static readonly Color Clipboard = Color.Parse("#14A085");   // teal
    private static readonly Color Storage = Color.Parse("#D08700");     // amber
    private static readonly Color Destructive = Color.Parse("#DC3E36"); // red
    private static readonly Color Warning = Color.Parse("#D97706");     // orange
    private static readonly Color Neutral = Color.Parse("#8A9199");     // grey

    private static readonly Dictionary<string, Color> ColorsByGlyph = new()
    {
        // Taking a shot
        [LucideIcons.camera] = Capture,
        [LucideIcons.monitor] = Capture,
        [LucideIcons.monitor_up] = Capture,
        [LucideIcons.app_window] = Capture,
        [LucideIcons.scan] = Capture,
        [LucideIcons.square] = Capture,
        [LucideIcons.square_dashed] = Capture,
        [LucideIcons.layers] = Capture,
        [LucideIcons.scroll_text] = Capture,
        [LucideIcons.maximize] = Capture,
        [LucideIcons.clock] = Capture,
        [LucideIcons.timer] = Capture,
        [LucideIcons.timer_off] = Capture,
        [LucideIcons.play] = Capture,
        [LucideIcons.circle_stop] = Capture,
        [LucideIcons.circle_check] = Capture,

        // Working on the image
        [LucideIcons.image] = Editing,
        [LucideIcons.image_up] = Editing,
        [LucideIcons.images] = Editing,
        [LucideIcons.file_image] = Editing,
        [LucideIcons.wand_sparkles] = Editing,
        [LucideIcons.sparkles] = Editing,
        [LucideIcons.ruler] = Editing,

        // Clipboard and copying
        [LucideIcons.clipboard] = Clipboard,
        [LucideIcons.clipboard_list] = Clipboard,
        [LucideIcons.copy] = Clipboard,
        [LucideIcons.files] = Clipboard,
        [LucideIcons.file] = Clipboard,
        [LucideIcons.file_text] = Clipboard,
        [LucideIcons.route] = Clipboard,

        // Files on disk
        [LucideIcons.folder] = Storage,
        [LucideIcons.folder_open] = Storage,
        [LucideIcons.external_link] = Storage,
        [LucideIcons.database] = Storage,

        // Removing things
        [LucideIcons.trash_2] = Destructive,
        [LucideIcons.file_x] = Destructive,
        [LucideIcons.list_x] = Destructive,

        // Attention
        [LucideIcons.triangle_alert] = Warning,
    };

    /// <summary>
    /// The colour for a glyph, or the neutral grey for anything unmapped
    /// (settings, hotkeys, about and other chrome, which read better muted).
    /// </summary>
    public static Color ColorForGlyph(string glyph)
    {
        return !string.IsNullOrEmpty(glyph) && ColorsByGlyph.TryGetValue(glyph, out Color color) ? color : Neutral;
    }

    public static IBrush BrushForGlyph(string glyph)
    {
        return new SolidColorBrush(ColorForGlyph(glyph));
    }
}
