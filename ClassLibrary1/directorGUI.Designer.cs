namespace ModForResearchTUB
{
    partial class directorGUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.camera_pos = new System.Windows.Forms.TextBox();
            this.label_cam_pos = new System.Windows.Forms.Label();
            this.camera_rot = new System.Windows.Forms.TextBox();
            this.button_left = new System.Windows.Forms.Button();
            this.button_right = new System.Windows.Forms.Button();
            this.button_forward = new System.Windows.Forms.Button();
            this.button_backward = new System.Windows.Forms.Button();
            this.label_cam_rot = new System.Windows.Forms.Label();
            this.button_turn_left = new System.Windows.Forms.Button();
            this.button_turn_right = new System.Windows.Forms.Button();
            this.button_upward = new System.Windows.Forms.Button();
            this.button_downward = new System.Windows.Forms.Button();
            this.button_fov_plus = new System.Windows.Forms.Button();
            this.button_fov_minus = new System.Windows.Forms.Button();
            this.label_fov = new System.Windows.Forms.Label();
            this.label_clone_cam = new System.Windows.Forms.Label();
            this.button_clone_cam = new System.Windows.Forms.Button();
            this.code_output = new System.Windows.Forms.RichTextBox();
            this.delete_camera = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // camera_pos
            // 
            this.camera_pos.Location = new System.Drawing.Point(31, 29);
            this.camera_pos.Name = "camera_pos";
            this.camera_pos.Size = new System.Drawing.Size(220, 20);
            this.camera_pos.TabIndex = 0;
            // 
            // label_cam_pos
            // 
            this.label_cam_pos.AutoSize = true;
            this.label_cam_pos.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label_cam_pos.Location = new System.Drawing.Point(31, 13);
            this.label_cam_pos.Name = "label_cam_pos";
            this.label_cam_pos.Size = new System.Drawing.Size(82, 13);
            this.label_cam_pos.TabIndex = 1;
            this.label_cam_pos.Text = "Camera position";
            // 
            // camera_rot
            // 
            this.camera_rot.Location = new System.Drawing.Point(31, 73);
            this.camera_rot.Name = "camera_rot";
            this.camera_rot.Size = new System.Drawing.Size(220, 20);
            this.camera_rot.TabIndex = 2;
            // 
            // button_left
            // 
            this.button_left.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_left.Location = new System.Drawing.Point(31, 179);
            this.button_left.Name = "button_left";
            this.button_left.Size = new System.Drawing.Size(60, 52);
            this.button_left.TabIndex = 3;
            this.button_left.Text = "←";
            this.button_left.UseVisualStyleBackColor = true;
            this.button_left.Click += new System.EventHandler(this.button_left_Click);
            // 
            // button_right
            // 
            this.button_right.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_right.Location = new System.Drawing.Point(165, 179);
            this.button_right.Name = "button_right";
            this.button_right.Size = new System.Drawing.Size(59, 52);
            this.button_right.TabIndex = 4;
            this.button_right.Text = "→";
            this.button_right.UseVisualStyleBackColor = true;
            this.button_right.Click += new System.EventHandler(this.button_right_Click);
            // 
            // button_forward
            // 
            this.button_forward.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_forward.Location = new System.Drawing.Point(100, 121);
            this.button_forward.Name = "button_forward";
            this.button_forward.Size = new System.Drawing.Size(59, 52);
            this.button_forward.TabIndex = 5;
            this.button_forward.Text = "↑";
            this.button_forward.UseVisualStyleBackColor = true;
            this.button_forward.Click += new System.EventHandler(this.button_forward_Click);
            // 
            // button_backward
            // 
            this.button_backward.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_backward.Location = new System.Drawing.Point(100, 179);
            this.button_backward.Name = "button_backward";
            this.button_backward.Size = new System.Drawing.Size(59, 52);
            this.button_backward.TabIndex = 6;
            this.button_backward.Text = "↓";
            this.button_backward.UseVisualStyleBackColor = true;
            this.button_backward.Click += new System.EventHandler(this.button_backward_Click);
            // 
            // label_cam_rot
            // 
            this.label_cam_rot.AutoSize = true;
            this.label_cam_rot.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label_cam_rot.Location = new System.Drawing.Point(31, 52);
            this.label_cam_rot.Name = "label_cam_rot";
            this.label_cam_rot.Size = new System.Drawing.Size(86, 13);
            this.label_cam_rot.TabIndex = 7;
            this.label_cam_rot.Text = "Camera Rotation";
            // 
            // button_turn_left
            // 
            this.button_turn_left.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_turn_left.Location = new System.Drawing.Point(31, 121);
            this.button_turn_left.Name = "button_turn_left";
            this.button_turn_left.Size = new System.Drawing.Size(60, 52);
            this.button_turn_left.TabIndex = 8;
            this.button_turn_left.Text = "⟲";
            this.button_turn_left.UseVisualStyleBackColor = true;
            this.button_turn_left.Click += new System.EventHandler(this.button_turn_left_Click);
            // 
            // button_turn_right
            // 
            this.button_turn_right.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_turn_right.Location = new System.Drawing.Point(165, 121);
            this.button_turn_right.Name = "button_turn_right";
            this.button_turn_right.Size = new System.Drawing.Size(59, 52);
            this.button_turn_right.TabIndex = 9;
            this.button_turn_right.Text = "⟳";
            this.button_turn_right.UseVisualStyleBackColor = true;
            this.button_turn_right.Click += new System.EventHandler(this.button_turn_right_Click);
            // 
            // button_upward
            // 
            this.button_upward.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_upward.Location = new System.Drawing.Point(99, 237);
            this.button_upward.Name = "button_upward";
            this.button_upward.Size = new System.Drawing.Size(60, 52);
            this.button_upward.TabIndex = 10;
            this.button_upward.Text = "↟";
            this.button_upward.UseVisualStyleBackColor = true;
            this.button_upward.Click += new System.EventHandler(this.button_upward_Click);
            // 
            // button_downward
            // 
            this.button_downward.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_downward.Location = new System.Drawing.Point(100, 295);
            this.button_downward.Name = "button_downward";
            this.button_downward.Size = new System.Drawing.Size(59, 52);
            this.button_downward.TabIndex = 11;
            this.button_downward.Text = "↡";
            this.button_downward.UseVisualStyleBackColor = true;
            this.button_downward.Click += new System.EventHandler(this.button_downward_Click);
            // 
            // button_fov_plus
            // 
            this.button_fov_plus.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_fov_plus.Location = new System.Drawing.Point(253, 176);
            this.button_fov_plus.Name = "button_fov_plus";
            this.button_fov_plus.Size = new System.Drawing.Size(59, 52);
            this.button_fov_plus.TabIndex = 12;
            this.button_fov_plus.Text = "+";
            this.button_fov_plus.UseVisualStyleBackColor = true;
            // 
            // button_fov_minus
            // 
            this.button_fov_minus.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_fov_minus.Location = new System.Drawing.Point(318, 176);
            this.button_fov_minus.Name = "button_fov_minus";
            this.button_fov_minus.Size = new System.Drawing.Size(59, 52);
            this.button_fov_minus.TabIndex = 13;
            this.button_fov_minus.Text = "-";
            this.button_fov_minus.UseVisualStyleBackColor = true;
            // 
            // label_fov
            // 
            this.label_fov.AutoSize = true;
            this.label_fov.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_fov.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label_fov.Location = new System.Drawing.Point(247, 121);
            this.label_fov.Name = "label_fov";
            this.label_fov.Size = new System.Drawing.Size(169, 31);
            this.label_fov.TabIndex = 14;
            this.label_fov.Text = "Field of View";
            // 
            // label_clone_cam
            // 
            this.label_clone_cam.AutoSize = true;
            this.label_clone_cam.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_clone_cam.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label_clone_cam.Location = new System.Drawing.Point(247, 251);
            this.label_clone_cam.Name = "label_clone_cam";
            this.label_clone_cam.Size = new System.Drawing.Size(235, 31);
            this.label_clone_cam.TabIndex = 15;
            this.label_clone_cam.Text = "Clone current cam";
            // 
            // button_clone_cam
            // 
            this.button_clone_cam.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_clone_cam.Location = new System.Drawing.Point(253, 295);
            this.button_clone_cam.Name = "button_clone_cam";
            this.button_clone_cam.Size = new System.Drawing.Size(59, 52);
            this.button_clone_cam.TabIndex = 16;
            this.button_clone_cam.Text = "❐";
            this.button_clone_cam.UseVisualStyleBackColor = true;
            this.button_clone_cam.Click += new System.EventHandler(this.button_clone_cam_Click);
            // 
            // code_output
            // 
            this.code_output.Location = new System.Drawing.Point(12, 373);
            this.code_output.Name = "code_output";
            this.code_output.Size = new System.Drawing.Size(502, 214);
            this.code_output.TabIndex = 18;
            this.code_output.Text = "";
            // 
            // delete_camera
            // 
            this.delete_camera.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.delete_camera.Location = new System.Drawing.Point(318, 295);
            this.delete_camera.Name = "delete_camera";
            this.delete_camera.Size = new System.Drawing.Size(59, 52);
            this.delete_camera.TabIndex = 19;
            this.delete_camera.Text = "✖";
            this.delete_camera.UseVisualStyleBackColor = true;
            this.delete_camera.Click += new System.EventHandler(this.delete_camera_Click);
            // 
            // directorGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(526, 599);
            this.Controls.Add(this.delete_camera);
            this.Controls.Add(this.code_output);
            this.Controls.Add(this.button_clone_cam);
            this.Controls.Add(this.label_clone_cam);
            this.Controls.Add(this.label_fov);
            this.Controls.Add(this.button_fov_minus);
            this.Controls.Add(this.button_fov_plus);
            this.Controls.Add(this.button_downward);
            this.Controls.Add(this.button_upward);
            this.Controls.Add(this.button_turn_right);
            this.Controls.Add(this.button_turn_left);
            this.Controls.Add(this.label_cam_rot);
            this.Controls.Add(this.button_backward);
            this.Controls.Add(this.button_forward);
            this.Controls.Add(this.button_right);
            this.Controls.Add(this.button_left);
            this.Controls.Add(this.camera_rot);
            this.Controls.Add(this.label_cam_pos);
            this.Controls.Add(this.camera_pos);
            this.Name = "directorGUI";
            this.Text = "directorGUI";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox camera_pos;
        private System.Windows.Forms.Label label_cam_pos;
        private System.Windows.Forms.TextBox camera_rot;
        private System.Windows.Forms.Button button_left;
        private System.Windows.Forms.Button button_right;
        private System.Windows.Forms.Button button_forward;
        private System.Windows.Forms.Button button_backward;
        private System.Windows.Forms.Label label_cam_rot;
        private System.Windows.Forms.Button button_turn_left;
        private System.Windows.Forms.Button button_turn_right;
        private System.Windows.Forms.Button button_upward;
        private System.Windows.Forms.Button button_downward;
        private System.Windows.Forms.Button button_fov_plus;
        private System.Windows.Forms.Button button_fov_minus;
        private System.Windows.Forms.Label label_fov;
        private System.Windows.Forms.Label label_clone_cam;
        private System.Windows.Forms.Button button_clone_cam;
        private System.Windows.Forms.RichTextBox code_output;
        private System.Windows.Forms.Button delete_camera;
    }
}