/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2016 Frederic Chaxel <fchaxel@free.fr>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

// A dialog form for simple control input : textbox, combo, list, numeric, date Time, trackbar ....
// latest adapation should be done using the delegate parameters

/*
  var Input =
  new GenericInputBox<NumericUpDown>("Add Instance", "Instance Id :",
      (o) =>
      {
          // adjustment to the generic control
          o.Minimum = 1; o.Maximum = 255; o.Value = Numbase;
      });
  DialogResult res = Input.ShowDialog();
  // Get the Value using the genericInputmember
  byte Id = (byte)Input.genericInput.Value;
  
*/

namespace System.Windows.Forms
{
    public delegate void PostInitializeComponent<T>(T generic);

    [System.ComponentModel.DesignerCategory("")] // Avoid failure opening with the GUI Designer
    public partial class GenericInputBox<T> : Form where T : Control, new()
    {
        public GenericInputBox(String BoxTitle, String Lbl, PostInitializeComponent<T> FillInput, double sizeFactor=1)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            InitializeComponent(sizeFactor);
            genericLbl.Text = Lbl;
            this.Text = BoxTitle;
            if (FillInput!=null)
                FillInput(genericInput); // 'Callback' for optional genericInput content initialization
        }

        private void bt_Click(object sender, EventArgs e)
        {
            if (sender==btOK)
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        public T genericInput;
        private System.Windows.Forms.Label genericLbl;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Button btCancel;

        private void InitializeComponent(double sizeFactor)
        {
            this.genericLbl = new System.Windows.Forms.Label();
            this.genericInput = new T();
            this.btOK = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // genericLbl
            // 
            this.genericLbl.AutoSize = true;
            this.genericLbl.Location = new System.Drawing.Point(21, 29);
            this.genericLbl.Name = "genericLbl";
            this.genericLbl.Size = new System.Drawing.Size(56, 13);
            this.genericLbl.TabIndex = 0;
            this.genericLbl.Text = "genericLbl";
            // 
            // genericInput
            // 
            this.genericInput.Location = new System.Drawing.Point(25, 45);
            this.genericInput.Size = new System.Drawing.Size((int)(155*sizeFactor), 20);
            this.genericInput.TabIndex = 1;
            // 
            // btOK
            // 
            this.btOK.Location = new System.Drawing.Point(25, 78);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(70, 23);
            this.btOK.TabIndex = 2;
            this.btOK.Text = "OK";
            this.btOK.UseVisualStyleBackColor = true;
            this.btOK.Click += new System.EventHandler(this.bt_Click);
            // 
            // btOK
            // 
            this.btCancel.Location = new System.Drawing.Point((int)(110 + (155 * (sizeFactor - 1))), 78);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(70, 23);
            this.btCancel.TabIndex = 2;
            this.btCancel.Text = "Cancel";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.bt_Click);
            // 
            // GenericInputBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size((int)(203 + (155 * (sizeFactor-1))), 133);
            this.Controls.Add(this.btOK);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.genericInput);
            this.Controls.Add(this.genericLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GenericInputBox";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }

}
