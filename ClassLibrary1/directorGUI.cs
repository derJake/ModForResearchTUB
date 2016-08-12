using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModForResearchTUB
{
    public partial class directorGUI : Form
    {
        public Utilities ut { get; set; }
        public String code_output_text { get; set;  }
        delegate void SetTextCallback(string text);
        private float amount = 0.9f;

        public directorGUI()
        {
            InitializeComponent();
        }

        private void button_left_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Left, amount);
        }

        private void button_forward_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Forward, amount);
        }

        private void button_right_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Right, amount);
        }

        private void button_backward_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Backward, amount);
        }

        private void button_upward_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Up, amount);
        }

        private void button_downward_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.Down, amount);
        }

        private void button_clone_cam_Click(object sender, EventArgs e)
        {
            //ut.cloneCamera();
            ut.activateScriptCam();
        }

        private void delete_camera_Click(object sender, EventArgs e)
        {
            ut.deleteScriptCams();
        }

        private void button_turn_left_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.TurnLeft, amount);
        }

        private void button_turn_right_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.TurnRight, amount);
        }

        private void button_pitch_plus_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.TurnUp, amount);
        }

        private void button_pitch_minus_Click(object sender, EventArgs e)
        {
            ut.moveCamera(Direction.TurnDown, amount);
        }

        private void code_output_TextChanged(object sender, EventArgs e)
        {

        }

        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.code_output.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.code_output.Text = text;
            }
        }
    }
}
