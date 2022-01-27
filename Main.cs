using System.Net.Sockets;
using System.Net;

namespace SecureSpeak
{
    

    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void network_worker()
        {
            IPEndPoint ep = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 1, 2 }), 8069);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ep);
            socket.Listen();


        }


        Random random = new Random();
        private void createNewChatBtn_Click(object sender, EventArgs e)
        {
            new ChatWidget(chatPanel, random.Next(3333333).ToString());
        }

    }


    class Message
    {

    }

    public class ChatWidget
    {
        Panel container;
        Panel panel;
        Label username;
        Label description;
        PictureBox profile_pic;
        Button remove_btn;

        private string newUUID()
        {
            return Guid.NewGuid().ToString();
        }

        private void createPanel()
        {
            panel = new Panel();
            panel.BackColor = Color.FromArgb(133, 133, 133);
            panel.Size = new Size(198, 89);
            panel.AutoScroll = false;
            panel.VerticalScroll.Visible = false;
            panel.Name = newUUID();

            container.Controls.Add(panel);
        }

        private void createUsername(string username)
        {
            this.username = new Label();
            this.username.Text = username;
            this.username.Location = new Point(60, 3);
            this.username.MaximumSize = new Size(133, 13);
            this.username.Font = new Font(FontFamily.GenericSansSerif, 8.25f, FontStyle.Bold | FontStyle.Underline);
            this.username.AutoSize = true;
            this.username.AutoEllipsis = true;
            panel.Controls.Add(this.username);
        }

        private void createDescription()
        {
            description = new Label();
            description.Text = "data";
            description.MaximumSize = new Size(135, 40);
            description.Location = new Point(60, 16);
            description.AutoEllipsis = true;
            description.AutoSize = true;

            panel.Controls.Add(description);
        }

        private void createProfile()
        {
            profile_pic = new PictureBox();
            profile_pic.Location = new Point(5, 3);
            profile_pic.Size = new Size(55, 55);
            profile_pic.BackColor = Color.Aqua;

            panel.Controls.Add(profile_pic);
        }

        private void createRemoveButton()
        {
            remove_btn = new Button();

            remove_btn.Text = "Remove Chat";
            remove_btn.Location = new Point(3, 61);
            remove_btn.Size = new Size(192, 25);
            remove_btn.FlatStyle = FlatStyle.Flat;
            remove_btn.Click += (s, EventArgs) => { remove(); };

            panel.Controls.Add(remove_btn);
        }

        public ChatWidget(Panel container, string username)
        {
            this.container = container;
            createPanel();
            createUsername(username);
            createDescription();
            createProfile();
            createRemoveButton();
        }

        private void remove()
        {
            container.Controls.Remove(panel);
        }
    }
}