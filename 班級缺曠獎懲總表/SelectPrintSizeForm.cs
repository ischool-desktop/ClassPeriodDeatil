using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using System.Xml;
using FISCA.Presentation.Controls;
using FISCA.DSAUtil;

namespace ClassPeriodDetail
{
    public partial class SelectPrintSizeForm : BaseForm
    {
        string _ConfigPrint;

        Campus.Configuration.ConfigData cd;

        /// <summary>
        /// 傳入設定檔名稱與設定檔參數
        /// </summary>
        /// <param name="ConfigName">設定檔名稱</param>
        /// 

        public SelectPrintSizeForm(String ConfigPrint)
        {
            InitializeComponent();

            _ConfigPrint = ConfigPrint;

            cd = Campus.Configuration.Config.User[_ConfigPrint];
            string config = cd["紙張設定"];
            int x = 0;
            if (!string.IsNullOrEmpty(config))
            {
                //列印資訊
                int.TryParse(config, out x);
            }
            comboBoxEx1.SelectedIndex = x;
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            int x = comboBoxEx1.SelectedIndex;
            cd["紙張設定"] = x.ToString();
            cd.Save();
            this.Close();
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
