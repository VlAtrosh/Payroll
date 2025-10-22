using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
	public static class UiTheme
	{
		public class Palette
		{
			public Color WindowBack { get; set; }
			public Color Surface { get; set; }
			public Color SurfaceAlt { get; set; }
			public Color TextPrimary { get; set; }
			public Color TextSecondary { get; set; }
			public Color Accent { get; set; }
			public Color AccentText { get; set; }
		}

		public static class Palettes
		{
			public static readonly Palette Light = new Palette
			{
				WindowBack = Color.FromArgb(245, 247, 250),
				Surface = Color.White,
				SurfaceAlt = Color.FromArgb(240, 243, 247),
				TextPrimary = Color.FromArgb(28, 28, 30),
				TextSecondary = Color.FromArgb(100, 104, 109),
				Accent = Color.FromArgb(52, 120, 246),
				AccentText = Color.White
			};

			public static readonly Palette Dark = new Palette
			{
				WindowBack = Color.FromArgb(26, 27, 30),
				Surface = Color.FromArgb(34, 36, 40),
				SurfaceAlt = Color.FromArgb(44, 46, 52),
				TextPrimary = Color.FromArgb(230, 230, 235),
				TextSecondary = Color.FromArgb(160, 165, 175),
				Accent = Color.FromArgb(88, 166, 255),
				AccentText = Color.Black
			};
		}

		public static void ApplyTheme(Form form, Palette palette)
		{
			if (form == null || palette == null) return;

			form.BackColor = palette.WindowBack;
			form.ForeColor = palette.TextPrimary;
			// Единый шрифт по всему приложению, чтобы текст выглядел ровно
			var baseFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			if (!form.Font.Equals(baseFont)) form.Font = baseFont;

			ApplyThemeToControls(form.Controls, palette);
		}

		private static void ApplyThemeToControls(Control.ControlCollection controls, Palette palette)
		{
			foreach (Control control in controls)
			{
				// Синхронизируем шрифт
				if (!(control is DataGridView))
				{
					var baseFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
					if (!control.Font.Equals(baseFont)) control.Font = baseFont;
				}

				if (control is Panel || control is GroupBox || control is FlowLayoutPanel || control is TableLayoutPanel)
				{
					control.BackColor = palette.Surface;
					control.ForeColor = palette.TextPrimary;
				}
				else if (control is Button btn)
				{
					btn.FlatStyle = FlatStyle.Flat;
					btn.FlatAppearance.BorderSize = 0;
					btn.BackColor = palette.Accent;
					btn.ForeColor = palette.AccentText;
					var padding = new Padding(12, 6, 12, 6);
					btn.Padding = padding;
					btn.TextAlign = ContentAlignment.MiddleCenter;
					btn.UseCompatibleTextRendering = false;
					btn.AutoSize = true;
					btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
					btn.MinimumSize = new Size(100, 30);
					btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
						Math.Min(255, palette.Accent.R + 10),
						Math.Min(255, palette.Accent.G + 10),
						Math.Min(255, palette.Accent.B + 10));
				}
				else if (control is Label lbl)
				{
					lbl.BackColor = Color.Transparent;
					lbl.ForeColor = palette.TextSecondary;
					lbl.AutoSize = true;
				}
				else if (control is TextBox tb)
				{
					tb.BackColor = palette.SurfaceAlt;
					tb.ForeColor = palette.TextPrimary;
					tb.BorderStyle = BorderStyle.FixedSingle;
				}
				else if (control is MaskedTextBox mtb)
				{
					mtb.BackColor = palette.SurfaceAlt;
					mtb.ForeColor = palette.TextPrimary;
					mtb.BorderStyle = BorderStyle.FixedSingle;
				}
				else if (control is ComboBox cb)
				{
					cb.BackColor = palette.SurfaceAlt;
					cb.ForeColor = palette.TextPrimary;
					cb.FlatStyle = FlatStyle.Flat;
				}
				else if (control is DateTimePicker dtp)
				{
					dtp.CalendarMonthBackground = palette.SurfaceAlt;
					dtp.CalendarTitleBackColor = palette.Accent;
					dtp.CalendarTitleForeColor = palette.AccentText;
					dtp.BackColor = palette.SurfaceAlt;
					dtp.ForeColor = palette.TextPrimary;
				}
				else if (control is CheckBox chk)
				{
					chk.BackColor = Color.Transparent;
					chk.ForeColor = palette.TextPrimary;
				}
				else if (control is TabControl tabs)
				{
					tabs.BackColor = palette.Surface;
					tabs.ForeColor = palette.TextPrimary;
					tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
					tabs.Padding = new Point(18, 6);
					tabs.SizeMode = TabSizeMode.Fixed;
					tabs.ItemSize = new Size(120, 30);
					tabs.DrawItem += (s, e) =>
					{
						var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
						var back = selected ? palette.Accent : palette.SurfaceAlt;
						using (var br = new SolidBrush(back)) e.Graphics.FillRectangle(br, e.Bounds);
						var textColor = selected ? palette.AccentText : palette.TextPrimary;
						var rect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
						TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
					};
				}
				else if (control is DataGridView grid)
				{
					StyleGrid(grid, palette);
				}

				if (control.HasChildren)
					ApplyThemeToControls(control.Controls, palette);
			}
		}

		private static void StyleGrid(DataGridView grid, Palette palette)
		{
			grid.BackgroundColor = palette.Surface;
			grid.EnableHeadersVisualStyles = false;
			grid.BorderStyle = BorderStyle.None;
			grid.GridColor = palette.SurfaceAlt;

			grid.ColumnHeadersDefaultCellStyle.BackColor = palette.Accent;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.AccentText;
			grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.Accent;
			grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.AccentText;

			grid.DefaultCellStyle.BackColor = palette.Surface;
			grid.DefaultCellStyle.ForeColor = palette.TextPrimary;
			grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(
				Math.Min(255, palette.Accent.R + 10),
				Math.Min(255, palette.Accent.G + 10),
				Math.Min(255, palette.Accent.B + 10));
			grid.DefaultCellStyle.SelectionForeColor = palette.AccentText;

			grid.AlternatingRowsDefaultCellStyle.BackColor = palette.SurfaceAlt;
		}
	}
}


