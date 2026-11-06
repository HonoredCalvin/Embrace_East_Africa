using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace EmbraceEA
{
    public class MainForm : Form
    {
        // ---- colours (from the logo) ----
        static readonly Color Yellow = Color.FromArgb(255, 221, 0);
        static readonly Color Dark = Color.FromArgb(24, 24, 24);
        static readonly Color Red = Color.FromArgb(226, 35, 40);
        static readonly Color Green = Color.FromArgb(0, 140, 70);
        static readonly Color PageBg = Color.FromArgb(246, 244, 235);
        static readonly Color Muted = Color.FromArgb(95, 95, 95);

        // ---- my real details ----
        const string Email = "wasswa9110calvin@gmail.com";
        const string Phone = "+256 751 557 915";

        const int M = 30;       // left margin
        const int W = 1060;     // content width
        const string BannerText = "EMBRACE EA";

        string imgDir;
        string[] photos;
        int photoIndex = 0;
        int letters = 0;
        int userId = 0;         // 0 = not signed in

        PictureBox slide;
        Label banner;
        Button btnPause, signBtn;
        Timer slideTimer = new Timer();
        Timer bannerTimer = new Timer();

        public MainForm()
        {
            Text = "Embrace East Africa";
            ClientSize = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBg;
            Font = new Font("Segoe UI", 10f);
            AutoScroll = true;
            AutoScrollMinSize = new Size(1120, 1830);
            DoubleBuffered = true;

            imgDir = Path.Combine(Application.StartupPath, "Images");
            if (!Directory.Exists(imgDir)) imgDir = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\Images"));

            BuildHeader();
            BuildMenu();
            BuildSlideshow();
            BuildMapAndMission();
            BuildFlags();
            BuildContactAndLinks();
            BuildQuestionAndFaq();
            BuildRibbon();
        }

        protected override CreateParams CreateParams   // smoother scrolling
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
        }

        // =============== helpers ===============
        Image LoadImage(string file)
        {
            string p = Path.Combine(imgDir, file);
            if (!File.Exists(p)) return null;
            using (Image i = Image.FromFile(p)) { return new Bitmap(i); }
        }

        static void Round(Control c, int r)
        {
            int d = r * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(0, 0, d, d, 180, 90);
            p.AddArc(c.Width - d, 0, d, d, 270, 90);
            p.AddArc(c.Width - d, c.Height - d, d, d, 0, 90);
            p.AddArc(0, c.Height - d, d, d, 90, 90);
            p.CloseFigure();
            c.Region = new Region(p);
        }

        // White rounded card with a soft shadow
        Panel Card(int x, int y, int w, int h)
        {
            Panel shadow = new Panel();
            shadow.Bounds = new Rectangle(x + 2, y + 5, w, h);
            shadow.BackColor = Color.FromArgb(222, 218, 202);
            Round(shadow, 16);
            Controls.Add(shadow);

            Panel card = new Panel();
            card.Bounds = new Rectangle(x, y, w, h);
            card.BackColor = Color.White;
            Round(card, 16);
            Controls.Add(card);
            shadow.SendToBack();  
            return card;
        }

        Label AddLabel(Control parent, string text, int x, int y, int w, int h, float size, FontStyle style, ContentAlignment align)
        {
            Label l = new Label();
            l.Text = text; l.Location = new Point(x, y); l.Size = new Size(w, h);
            l.Font = new Font("Segoe UI", size, style); l.TextAlign = align;
            l.ForeColor = Dark;
            parent.Controls.Add(l);
            return l;
        }

        // Section title with a small yellow bar 
        void Heading(Control parent, string text, int x, int y)
        {
            AddLabel(parent, text, x, y, 500, 30, 15f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            Panel bar = new Panel();
            bar.BackColor = Yellow; bar.Bounds = new Rectangle(x + 3, y + 32, 48, 4);
            parent.Controls.Add(bar);
        }

        PictureBox AddPicture(Control parent, string file, int x, int y, int w, int h)
        {
            PictureBox p = new PictureBox();
            p.Location = new Point(x, y); p.Size = new Size(w, h);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.Image = LoadImage(file);
            parent.Controls.Add(p);
            return p;
        }

        // style: 0 = white/outline, 1 = yellow, 2 = dark
        Button AddButton(Control parent, string text, int x, int y, int w, int h, int style)
        {
            Button b = new Button();
            b.Text = text; b.Location = new Point(x, y); b.Size = new Size(w, h);
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            if (style == 1)
            {
                b.BackColor = Yellow; b.ForeColor = Dark;
                b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 235, 90);
            }
            else if (style == 2)
            {
                b.BackColor = Dark; b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            }
            else
            {
                b.BackColor = Color.White; b.ForeColor = Dark;
                b.FlatAppearance.BorderColor = Color.FromArgb(190, 190, 190);
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 244, 238);
            }
            Round(b, 8);
            parent.Controls.Add(b);
            return b;
        }

        LinkLabel AddLink(Control parent, string text, int x, int y, int w, int h, Color color, Color hover, EventHandler click)
        {
            LinkLabel l = new LinkLabel();
            l.Text = text; l.Location = new Point(x, y); l.Size = new Size(w, h);
            l.UseMnemonic = false;   // show '&' as a normal character
            l.LinkColor = color; l.ActiveLinkColor = hover; l.VisitedLinkColor = color;
            l.LinkBehavior = LinkBehavior.NeverUnderline;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.BackColor = Color.Transparent;
            l.Cursor = Cursors.Hand;
            l.MouseEnter += delegate { l.LinkColor = hover; };
            l.MouseLeave += delegate { l.LinkColor = color; };
            l.Click += click;
            parent.Controls.Add(l);
            return l;
        }

        TextBox AddTextBox(Control parent, int x, int y, int w, int h, bool multiline)
        {
            TextBox t = new TextBox();
            t.Multiline = multiline; t.Location = new Point(x, y); t.Size = new Size(w, h);
            t.BackColor = Color.FromArgb(251, 250, 245);
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = new Font("Segoe UI", 10.5f);
            if (multiline) t.ScrollBars = ScrollBars.Vertical;
            parent.Controls.Add(t);
            return t;
        }

        // =============== 1. Header: logo, title, tagline ===============
        void BuildHeader()
        {
            Panel band = new Panel();
            band.Bounds = new Rectangle(0, 0, 1120, 130);
            band.BackColor = Color.White;
            Controls.Add(band);

            AddPicture(band, @"Logo\logo.png", M, 8, 116, 110);
            AddLabel(band, "EMBRACE  EAST  AFRICA", 160, 8, 930, 62, 34f, FontStyle.Bold, ContentAlignment.MiddleCenter);
            Label tag = AddLabel(band, "ENGENDERING MOVEMENT, BILATERAL RELATIONS AND\r\nACCESS TO COMMERCE IN EAST-AFRICA",
                                 160, 70, 930, 48, 12f, FontStyle.Bold, ContentAlignment.MiddleCenter);
            tag.ForeColor = Muted;

            // four-colour strip from the logo
            Color[] cols = { Yellow, Red, Green, Dark };
            for (int i = 0; i < 4; i++)
            {
                Panel p = new Panel();
                p.BackColor = cols[i]; p.Bounds = new Rectangle(i * 280, 125, 280, 5);
                band.Controls.Add(p);
            }
        }

        // =============== 2. Menu ===============
        void BuildMenu()
        {
            Panel menu = new Panel();
            menu.Bounds = new Rectangle(M, 148, W, 50);
            menu.BackColor = Dark;
            Round(menu, 25);
            Controls.Add(menu);

            string[] items = { "Home", "Workshops", "Gallery", "Services", "News & Media", "About Us" };
            int x = 20;
            foreach (string item in items)
            {
                string key = item;
                LinkLabel l = AddLink(menu, key, x, 9, 112, 32, Color.White, Yellow, delegate { OpenPage(key); });
                l.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
                x += 122;
            }

            signBtn = AddButton(menu, "Sign In / Sign Up", W - 215, 7, 200, 36, 1);
            signBtn.Click += delegate { ShowSignIn(); };
        }

        // =============== 3. Photos in motion + animated EMBRACE EA ===============
        void BuildSlideshow()
        {
            slide = AddPicture(this, "", M, 218, W, 400);
            slide.BackColor = Color.Black;
            Round(slide, 18);

            string dir = Path.Combine(imgDir, "Slides");
            photos = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.jpg") : new string[0];
            Array.Sort(photos);
            if (photos.Length > 0) slide.Image = LoadImage(@"Slides\" + Path.GetFileName(photos[0]));

            banner = new Label();
            banner.Dock = DockStyle.Bottom; banner.Height = 66;
            banner.BackColor = Color.FromArgb(175, 0, 0, 0);
            banner.ForeColor = Yellow;
            banner.Font = new Font("Segoe UI", 28f, FontStyle.Bold);
            banner.TextAlign = ContentAlignment.MiddleCenter;
            slide.Controls.Add(banner);

            btnPause = AddButton(slide, "Pause", W - 105, 14, 88, 34, 1);
            btnPause.Click += delegate
            {
                slideTimer.Enabled = !slideTimer.Enabled;
                btnPause.Text = slideTimer.Enabled ? "Pause" : "Play";
            };

            slideTimer.Interval = 3500;
            slideTimer.Tick += delegate
            {
                if (photos.Length == 0) return;
                photoIndex = (photoIndex + 1) % photos.Length;
                slide.Image = LoadImage(@"Slides\" + Path.GetFileName(photos[photoIndex]));
            };
            slideTimer.Start();

            // letters appear one after another, short pause, repeat
            bannerTimer.Interval = 330;
            bannerTimer.Tick += delegate
            {
                letters = (letters + 1) % (BannerText.Length + 4);
                string s = "";
                for (int i = 0; i < Math.Min(letters, BannerText.Length); i++) s += BannerText[i].ToString() + " ";
                banner.Text = s.TrimEnd();
            };
            bannerTimer.Start();
        }

        // =============== 4. Map + Mission, Vision, Core Values ===============
        void BuildMapAndMission()
        {
            Panel mapCard = Card(M, 640, 520, 300);
            AddPicture(mapCard, "map.png", 10, 10, 500, 280);

            Panel mv = Card(M + 540, 640, 520, 300);
            Section(mv, "MISSION", "To engender movement, strengthen bilateral relations and open access to commerce across East Africa.", Red, 14);
            Section(mv, "VISION", "A united, prosperous and integrated East Africa where every citizen embraces the region as one home.", Green, 102);
            Section(mv, "CORE VALUES", "Unity  \u2022  Integrity  \u2022  Inclusivity  \u2022  Collaboration  \u2022  Respect for Diversity", Yellow, 190);
        }

        void Section(Control parent, string title, string text, Color accent, int y)
        {
            Panel bar = new Panel();
            bar.BackColor = accent; bar.Bounds = new Rectangle(24, y + 4, 6, 22);
            parent.Controls.Add(bar);
            AddLabel(parent, title, 40, y, 440, 30, 13f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            Label t = AddLabel(parent, text, 40, y + 32, 460, 52, 10.5f, FontStyle.Regular, ContentAlignment.TopLeft);
            t.ForeColor = Muted;
        }

        // =============== 5. Seven flags (loaded from the database) ===============
        void BuildFlags()
        {
            Heading(this, "Our Member States", M, 958);

            DataTable countries = Db.Countries();
            for (int i = 0; i < countries.Rows.Count && i < 7; i++)
            {
                DataRow row = countries.Rows[i];
                int x = M + i * 153;
                Panel card = Card(x, 1008, 140, 160);

                PictureBox flag = AddPicture(card, @"Flags\" + row["FlagFile"], 10, 12, 120, 78);
                flag.Cursor = Cursors.Hand;
                flag.Click += delegate { ShowCountry(row); };

                Label name = AddLabel(card, row["Name"].ToString(), 0, 92, 140, 24, 10.5f, FontStyle.Bold, ContentAlignment.MiddleCenter);
                Button info = AddButton(card, "Click For Info", 14, 122, 112, 28, 1);
                info.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                info.Click += delegate { ShowCountry(row); };
            }
        }

        // =============== 6. Get in touch | Common links ===============
        void BuildContactAndLinks()
        {
            Panel touch = Card(M, 1198, 640, 250);
            Heading(touch, "Get in Touch with Us", 22, 14);
            AddLabel(touch, "Type a Message:", 22, 66, 125, 26, 10f, FontStyle.Regular, ContentAlignment.MiddleLeft);
            TextBox msg = AddTextBox(touch, 150, 62, 468, 108, true);
            Button cancel = AddButton(touch, "Cancel", 150, 190, 100, 36, 0);
            Button clear = AddButton(touch, "Clear", 330, 190, 100, 36, 0);
            Button send = AddButton(touch, "Send", 518, 190, 100, 36, 1);
            cancel.Click += delegate { msg.Clear(); AutoScrollPosition = new Point(0, 0); };
            clear.Click += delegate { msg.Clear(); msg.Focus(); };
            send.Click += delegate
            {
                if (msg.Text.Trim().Length == 0) { MessageBox.Show("Please type a message first.", "Embrace EA"); return; }
                if (SaveText("Messages", msg.Text.Trim())) { MessageBox.Show("Thank you! Your message has been sent.", "Embrace EA"); msg.Clear(); }
            };

            Panel links = Card(M + 660, 1198, 400, 250);
            Heading(links, "Common Links for All Countries", 22, 14);
            string[] names = { "East African Anthem", "East African Heads of States", "Learn Swahili", "About East African Passport" };
            for (int i = 0; i < names.Length; i++)
            {
                string key = names[i];
                Panel dot = new Panel();
                dot.BackColor = i % 2 == 0 ? Red : Green; dot.Bounds = new Rectangle(30, 80 + i * 40 + 11, 8, 8);
                links.Controls.Add(dot);
                LinkLabel l = AddLink(links, key, 48, 80 + i * 40, 330, 30, Dark, Red, delegate { OpenPage(key); });
                l.TextAlign = ContentAlignment.MiddleLeft;
                l.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);
            }
        }

        // =============== 7. Ask a question | FAQ (from the database) ===============
        void BuildQuestionAndFaq()
        {
            Panel ask = Card(M, 1478, 640, 230);
            Heading(ask, "Ask a Question", 22, 14);
            TextBox q = AddTextBox(ask, 22, 62, 470, 144, true);
            Button send = AddButton(ask, "Send", 510, 62, 108, 38, 1);
            Button clear = AddButton(ask, "Clear", 510, 110, 108, 38, 0);
            clear.Click += delegate { q.Clear(); q.Focus(); };
            send.Click += delegate
            {
                if (q.Text.Trim().Length == 0) { MessageBox.Show("Please type your question first.", "Embrace EA"); return; }
                if (SaveText("Questions", q.Text.Trim())) { MessageBox.Show("Thank you! We will get back to you soon.", "Embrace EA"); q.Clear(); }
            };

            Panel faq = Card(M + 660, 1478, 400, 230);
            Heading(faq, "FAQ", 22, 14);
            DataTable t = Db.Faqs();
            ListBox list = new ListBox();
            list.Font = new Font("Segoe UI", 11f);
            list.Location = new Point(22, 64); list.Size = new Size(358, 150);
            list.BorderStyle = BorderStyle.None; list.BackColor = Color.White;
            list.ItemHeight = 28; list.DrawMode = DrawMode.OwnerDrawFixed;
            foreach (DataRow r in t.Rows) list.Items.Add(r["Question"].ToString());
            list.DrawItem += delegate(object s, DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                bool sel = (e.State & DrawItemState.Selected) != 0;
                using (SolidBrush bg = new SolidBrush(sel ? Yellow : Color.White)) e.Graphics.FillRectangle(bg, e.Bounds);
                TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), list.Font,
                    new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 3, e.Bounds.Width - 6, e.Bounds.Height), Dark);
            };
            list.Click += delegate
            {
                if (list.SelectedIndex >= 0)
                    MessageBox.Show(t.Rows[list.SelectedIndex]["Answer"].ToString(), list.SelectedItem.ToString());
            };
            faq.Controls.Add(list);
        }

        // =============== 8. Lower ribbon ===============
        void BuildRibbon()
        {
            Panel ribbon = new Panel();
            ribbon.Bounds = new Rectangle(0, 1738, 1120, 92);
            ribbon.BackColor = Dark;
            Controls.Add(ribbon);

            Panel line = new Panel();
            line.BackColor = Yellow; line.Dock = DockStyle.Top; line.Height = 4;
            ribbon.Controls.Add(line);

            Label l = AddLabel(ribbon, "EMBRACE EAST AFRICA   |   Email: " + Email + "   |   Tel: " + Phone,
                               M, 18, 640, 28, 10.5f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            l.ForeColor = Yellow;

            Label status = AddLabel(ribbon, "\u00A9 " + DateTime.Now.Year + " Embrace East Africa   |   " +
                                    (Db.Ready ? "Database connected" : "Database offline (demo mode)"),
                                    M, 50, 640, 22, 9f, FontStyle.Regular, ContentAlignment.MiddleLeft);
            status.ForeColor = Db.Ready ? Color.FromArgb(120, 220, 150) : Color.Silver;

            string[] glyphs = { "f", "X", "in", "yt", "ig" };
            string[] urls = { "https://www.facebook.com/", "https://x.com/", "https://www.linkedin.com/",
                              "https://www.youtube.com/", "https://www.instagram.com/" };
            for (int i = 0; i < glyphs.Length; i++)
            {
                string url = urls[i];
                Button b = AddButton(ribbon, glyphs[i], 780 + i * 56, 26, 40, 40, 0);
                b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                Round(b, 20);
                b.Click += delegate { try { Process.Start(url); } catch { } };
            }
        }

        // =============== actions ===============
        bool SaveText(string table, string text)
        {
            string err = Db.Save("INSERT INTO " + table + " (UserId, Body) VALUES (@u, @b)",
                                 "@u", userId == 0 ? null : (object)userId, "@b", text);
            if (err != null) { MessageBox.Show(err, "Embrace EA"); return false; }
            return true;
        }

        void OpenPage(string key)
        {
            if (key == "Home") { AutoScrollPosition = new Point(0, 0); return; }
            if (key == "Gallery") { ShowGallery(); return; }
            if (key == "Workshops" || key == "News & Media")
            {
                if (!Db.Ready) { MessageBox.Show(Db.NotReadyMessage, "Embrace EA"); return; }
                DataTable t = (key == "Workshops")
                    ? Db.Query("SELECT Title, EventDate AS [Date], Venue, Description FROM Workshops ORDER BY EventDate")
                    : Db.Query("SELECT Title, PostedOn AS [Posted], Body FROM News ORDER BY PostedOn DESC");
                if (t.Rows.Count == 0)
                    ShowText(key, "Nothing has been posted yet.\r\n\r\nRows added to the " + (key == "Workshops" ? "Workshops" : "News") + " table in the database will appear here.");
                else ShowTable(key, t);
                return;
            }
            ShowText(key, Db.PageBody(key));
        }

        // =============== dialog windows ===============
        Form NewDialog(string title, int w, int h)
        {
            Form f = new Form();
            f.Text = title; f.ClientSize = new Size(w, h);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.CenterParent;
            f.BackColor = Color.White; f.Font = new Font("Segoe UI", 10f);
            Panel strip = new Panel();
            strip.BackColor = Yellow; strip.Dock = DockStyle.Top; strip.Height = 8;
            f.Controls.Add(strip);
            return f;
        }

        Button CloseButton(Form f)
        {
            Button b = AddButton(f, "Close", f.ClientSize.Width - 124, f.ClientSize.Height - 54, 100, 36, 1);
            b.DialogResult = DialogResult.OK;
            f.AcceptButton = b;
            return b;
        }

        void ShowCountry(DataRow r)
        {
            Form f = NewDialog(r["Name"].ToString(), 560, 380);
            PictureBox flag = AddPicture(f, @"Flags\" + r["FlagFile"], 24, 34, 200, 134);
            flag.BorderStyle = BorderStyle.FixedSingle;
            AddLabel(f, r["Name"].ToString(), 245, 30, 300, 46, 22f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            AddLabel(f, "Capital:  " + r["Capital"] + "\r\n\r\nEAC status:  " + r["Joined"] + "\r\n\r\nLanguages:  " + r["Languages"],
                     247, 84, 295, 130, 10f, FontStyle.Regular, ContentAlignment.TopLeft);
            Label about = AddLabel(f, r["About"].ToString(), 24, 205, 512, 90, 11f, FontStyle.Regular, ContentAlignment.TopLeft);
            about.ForeColor = Muted;
            CloseButton(f);
            f.ShowDialog(this);
        }

        void ShowText(string title, string body)
        {
            Form f = NewDialog(title, 560, 440);
            AddLabel(f, title, 24, 24, 510, 44, 20f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            TextBox t = new TextBox();
            t.Multiline = true; t.ReadOnly = true; t.ScrollBars = ScrollBars.Vertical;
            t.BorderStyle = BorderStyle.None; t.BackColor = Color.White;
            t.Font = new Font("Segoe UI", 11f);
            t.Bounds = new Rectangle(26, 84, 510, 280);
            t.Text = body; t.SelectionStart = 0; t.SelectionLength = 0;
            f.Controls.Add(t);
            CloseButton(f);
            f.ShowDialog(this);
        }

        void ShowTable(string title, DataTable data)
        {
            Form f = NewDialog(title, 780, 470);
            AddLabel(f, title, 24, 24, 700, 44, 20f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            DataGridView g = new DataGridView();
            g.Bounds = new Rectangle(24, 80, 732, 310);
            g.DataSource = data;
            g.ReadOnly = true; g.AllowUserToAddRows = false; g.RowHeadersVisible = false;
            g.BackgroundColor = Color.White; g.BorderStyle = BorderStyle.None;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = Yellow;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Dark;
            f.Controls.Add(g);
            CloseButton(f);
            f.ShowDialog(this);
        }

        void ShowGallery()
        {
            Form f = NewDialog("Gallery", 960, 620);
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Bounds = new Rectangle(10, 20, 940, 590);
            flow.AutoScroll = true;
            foreach (string file in photos)
            {
                PictureBox p = new PictureBox();
                p.Size = new Size(290, 200); p.Margin = new Padding(8);
                p.SizeMode = PictureBoxSizeMode.Zoom; p.BackColor = PageBg;
                p.Image = LoadImage(@"Slides\" + Path.GetFileName(file));
                flow.Controls.Add(p);
            }
            f.Controls.Add(flow);
            f.ShowDialog(this);
        }

        void ShowSignIn()
        {
            if (userId != 0)    // already signed in: this button signs out
            {
                userId = 0; signBtn.Text = "Sign In / Sign Up";
                MessageBox.Show("You have signed out.", "Embrace EA");
                return;
            }

            Form f = NewDialog("Sign In / Sign Up", 400, 410);
            Label title = AddLabel(f, "Sign In", 24, 22, 340, 40, 20f, FontStyle.Bold, ContentAlignment.MiddleLeft);
            CheckBox isNew = new CheckBox();
            isNew.Text = "I'm new - create an account"; isNew.Location = new Point(26, 68); isNew.Size = new Size(330, 24);
            f.Controls.Add(isNew);

            AddLabel(f, "Email address", 24, 104, 340, 22, 10f, FontStyle.Regular, ContentAlignment.MiddleLeft);
            TextBox tEmail = AddTextBox(f, 24, 128, 350, 28, false);
            AddLabel(f, "Password (at least 6 characters)", 24, 168, 340, 22, 10f, FontStyle.Regular, ContentAlignment.MiddleLeft);
            TextBox tPass = AddTextBox(f, 24, 192, 350, 28, false);
            tPass.UseSystemPasswordChar = true;
            Label lName = AddLabel(f, "Full name", 24, 232, 340, 22, 10f, FontStyle.Regular, ContentAlignment.MiddleLeft);
            TextBox tName = AddTextBox(f, 24, 256, 350, 28, false);
            lName.Visible = false; tName.Visible = false;

            Button go = AddButton(f, "Sign In", 24, 320, 350, 42, 1);
            f.AcceptButton = go;
            isNew.CheckedChanged += delegate
            {
                lName.Visible = tName.Visible = isNew.Checked;
                title.Text = go.Text = isNew.Checked ? "Sign Up" : "Sign In";
            };
            go.Click += delegate
            {
                string email = tEmail.Text.Trim();
                if (email.IndexOf('@') < 1 || tPass.Text.Length < 6)
                {
                    MessageBox.Show(f, "Enter a valid email and a password of at least 6 characters."); return;
                }
                if (!Db.Ready) { MessageBox.Show(f, Db.NotReadyMessage); return; }
                if (isNew.Checked)
                {
                    if (tName.Text.Trim().Length == 0) { MessageBox.Show(f, "Please enter your full name."); return; }
                    string err = Db.Register(tName.Text.Trim(), email, tPass.Text);
                    if (err != null) { MessageBox.Show(f, err); return; }
                }
                int id; string name;
                if (!Db.Login(email, tPass.Text, out id, out name)) { MessageBox.Show(f, "Wrong email or password."); return; }
                userId = id;
                signBtn.Text = "Hi, " + name.Split(' ')[0] + " (Sign out)";
                f.Close();
            };
            f.ShowDialog(this);
        }
    }
}
