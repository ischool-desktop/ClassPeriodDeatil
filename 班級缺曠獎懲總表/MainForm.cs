using Aspose.Cells;
using FISCA.DSAUtil;
using FISCA.Presentation.Controls;
using K12.Data;
using SHSchool.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ClassPeriodDetail
{
    public partial class MainForm : BaseForm
    {
        int _Schoolyear, _Semester;
        BackgroundWorker BGW;
        Workbook _WK;
        Dictionary<int, int> _CountAllColumnValue; //建立存放各項目加總的字典
        Dictionary<String, String> _ClassNameDic; //班級名稱字典
        /// <summary>
        /// 目前僅記錄列印紙張尺寸
        /// </summary>
        public string ConfigPrint = "班級缺曠獎懲總表(含功過相抵)_列印設定";
        /// <summary>
        /// XML結構之設定檔
        /// </summary>
        public string ConfigType = "班級缺曠獎懲總表(含功過相抵)_假別設定";

        public MainForm()
        {
            InitializeComponent();
            _Schoolyear = 90;
            _Semester = 1;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            intSchoolYear.Value = int.Parse(K12.Data.School.DefaultSchoolYear);
            intSemester.Value = int.Parse(K12.Data.School.DefaultSemester);
            _ClassNameDic = new Dictionary<string, string>();

            BGW = new BackgroundWorker();
            BGW.WorkerReportsProgress = true;
            BGW.DoWork += new DoWorkEventHandler(BGW_DoWork);
            BGW.ProgressChanged += new ProgressChangedEventHandler(BGW_ProgressChanged);
            BGW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_RunWorkerCompleted);
        }

        private void BGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            FormControl(true); //解除畫面元件鎖定
            SaveFileDialog sd = new System.Windows.Forms.SaveFileDialog();
            sd.Title = "另存新檔";
            sd.FileName = "班級缺曠獎懲總表.xls";
            sd.Filter = "Excel檔案 (*.xls)|*.xls|所有檔案 (*.*)|*.*";
            if (sd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _WK.Save(sd.FileName);
                    System.Diagnostics.Process.Start(sd.FileName);

                }
                catch
                {
                    FISCA.Presentation.Controls.MsgBox.Show("指定路徑無法存取。", "建立檔案失敗", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    this.Enabled = true;
                    return;
                }
            }
        }

        private void BGW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage("" + e.UserState, e.ProgressPercentage); //進度回報
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            if (!BGW.IsBusy)
            {
                _Schoolyear = intSchoolYear.Value;
                _Semester = intSemester.Value;
                FormControl(false); //鎖定畫面元件
                BGW.RunWorkerAsync();
            }
            else
            {
                MsgBox.Show("系統忙碌中,請稍後再試!!");
            }
        }

        private void BGW_DoWork(object sender, DoWorkEventArgs e)
        {
            //取得資料
            BGW.ReportProgress(10, "取得所選班級");
            #region 取得使用者所選擇的班級學生
            //取得所選班級紀錄
            List<ClassRecord> allClasses = Class.SelectByIDs(K12.Presentation.NLDPanels.Class.SelectedSource);

            //從班級紀錄取得學生清單
            List<StudentRecord> studentList = new List<StudentRecord>(); //學生記錄清單
            List<String> StudentIDList = new List<string>(); //學生ID清單
            foreach (ClassRecord classrecord in allClasses)
            {
                if(!_ClassNameDic.ContainsKey(classrecord.ID)) //儲存班級ID及Name方便往後查詢
                {
                    _ClassNameDic.Add(classrecord.ID, classrecord.Name);
                }

                foreach (StudentRecord student in classrecord.Students) //取得班級學生
                {
                    //只取得狀態為一般及延修的學生
                    if (student.Status == StudentRecord.StudentStatus.一般 || student.Status == StudentRecord.StudentStatus.延修)
                    {
                        studentList.Add(student);
                        StudentIDList.Add(student.ID);
                    }
                }
            }

            //建立班級字典存放各班級的學生
            Dictionary<String, List<StudentRecord>> classDic = new Dictionary<string, List<StudentRecord>>();
            
            foreach (StudentRecord student in studentList)
            {
                if (!classDic.ContainsKey(student.RefClassID)) //若該班級ID不存在就建立key
                {
                    classDic.Add(student.RefClassID, new List<StudentRecord>());
                }

                classDic[student.RefClassID].Add(student); //按對應班級ID將學生加入
            }

            int totalStudent = studentList.Count; //全部學生的總數,進度回報用

            foreach (String classid in classDic.Keys)
            {
                classDic[classid].Sort(SortStudent); //按學生座號排序字典內的清單
            }
            #endregion

            BGW.ReportProgress(20, "取得資料紀錄");

            #region 取得獎懲和缺曠紀錄
            //獎勵紀錄
            Dictionary<string, RewardRecord> MeritDemeritAttDic = new Dictionary<string, RewardRecord>();
            foreach (String id in StudentIDList) //建立清單中全部學生的獎懲紀錄字典
            {
                if (!MeritDemeritAttDic.ContainsKey(id))
                {
                    MeritDemeritAttDic.Add(id, new RewardRecord());
                }
            }

            foreach (SHMeritRecord each in SHMerit.SelectByStudentIDs(StudentIDList))
            {
                if (each.SchoolYear != _Schoolyear || each.Semester != _Semester)
                    continue;

                MeritDemeritAttDic[each.RefStudentID].MeritACount += each.MeritA.HasValue ? each.MeritA.Value : 0;
                MeritDemeritAttDic[each.RefStudentID].MeritBCount += each.MeritB.HasValue ? each.MeritB.Value : 0;
                MeritDemeritAttDic[each.RefStudentID].MeritCCount += each.MeritC.HasValue ? each.MeritC.Value : 0;
            }

            //懲罰紀錄
            foreach (SHDemeritRecord each in SHDemerit.SelectByStudentIDs(StudentIDList))
            {
                if (each.SchoolYear != _Schoolyear || each.Semester != _Semester)
                    continue;

                if (each.Cleared == "是")
                    continue;

                //留查紀錄
                if (each.MeritFlag == "2")
                    MeritDemeritAttDic[each.RefStudentID].Flag = true;

                MeritDemeritAttDic[each.RefStudentID].DemeritACount += each.DemeritA.HasValue ? each.DemeritA.Value : 0;
                MeritDemeritAttDic[each.RefStudentID].DemeritBCount += each.DemeritB.HasValue ? each.DemeritB.Value : 0;
                MeritDemeritAttDic[each.RefStudentID].DemeritCCount += each.DemeritC.HasValue ? each.DemeritC.Value : 0;
            }

            //取得有效的節次清單
            List<String> periodList = new List<string>();
            foreach (PeriodMappingInfo var in PeriodMapping.SelectAll())
            {
                if (!periodList.Contains(var.Name))
                {
                    periodList.Add(var.Name);
                }
            }

            ////取得影響缺曠紀錄的假別清單
            List<AbsenceMappingInfo> infoList = K12.Data.AbsenceMapping.SelectAll();
            List<String> Absence = new List<string>();

            foreach (AbsenceMappingInfo info in infoList)
            {
                if (!info.Noabsence) //若該假別會影響全勤就加入清單
                {
                    if (!Absence.Contains(info.Name))
                    {
                        Absence.Add(info.Name);
                    }
                }
            }

            //缺曠紀錄
            foreach (SHAttendanceRecord each in SHAttendance.SelectByStudentIDs(StudentIDList))
            {
                if (each.SchoolYear != _Schoolyear || each.Semester != _Semester)
                    continue;

                foreach (AttendancePeriod _Period in each.PeriodDetail)
                {
                    if (periodList.Contains(_Period.Period)) //確認是否有此節次
                    {
                        string typename = _Period.AbsenceType;
                        if (Absence.Contains(typename)) //如果此缺曠紀錄的假別會影響全勤,該學生的前勤紀錄則為false
                        {
                            MeritDemeritAttDic[each.RefStudentID].全勤 = false;
                        }

                        if (MeritDemeritAttDic[each.RefStudentID].Attendance.ContainsKey(typename)) //若該學生有此缺曠紀錄的類別
                        {
                            MeritDemeritAttDic[each.RefStudentID].Attendance[typename]++;
                        }
                        else
                        {
                            MeritDemeritAttDic[each.RefStudentID].Attendance.Add(typename, 1);
                        }
                    }

                }
            }
            #endregion

            //產生表格
            BGW.ReportProgress(30, "產生表格");
            #region 產生表格
            Workbook template = new Workbook();
            Workbook prototype = new Workbook();

            //取得列印紙張
            int sizeIndex = GetSizeIndex();
            //取得需列印的項目清單
            List<String> DisplayList = GetUserType();

            //列印尺寸
            if (sizeIndex == 0)
                template.Open(new MemoryStream(Properties.Resources.班級缺曠獎懲總表A3), FileFormatType.Excel2003);
            else if (sizeIndex == 1)
                template.Open(new MemoryStream(Properties.Resources.班級缺曠獎懲總表A4), FileFormatType.Excel2003);
            else if (sizeIndex == 2)
                template.Open(new MemoryStream(Properties.Resources.班級缺曠獎懲總表B4), FileFormatType.Excel2003);

            prototype.Copy(template);

            Worksheet prototypeSheet;
            int page = 1;
            foreach (String id in classDic.Keys)
            {
                prototype.Worksheets.AddCopy(0); //依照所選的班級數量新增分頁
                prototypeSheet = prototype.Worksheets[page]; //從第二個分頁開始畫製表格,page++;
                prototypeSheet.Name = GetClassName(id); //sheet.Name = 班級名稱

                //每5行加一條分隔線
                Range eachFiveLine = prototype.Worksheets[0].Cells.CreateRange(2, 5, false); //從第一個sheet複製
                for (int i = 2; i < classDic[id].Count + 2; i += 5)  //依照該班級學生數給予適量的分隔線
                {
                    prototypeSheet.Cells.CreateRange(i, 5, false).Copy(eachFiveLine);
                }

                //假別新增欄位
                Range addColumn = prototypeSheet.Cells.CreateRange(3, 1, true);
                for (int i = 19; i < 19 + DisplayList.Count; i++) //依照勾選的顯示清單新增欄位
                {
                    prototypeSheet.Cells.CreateRange(i, 1, true).Copy(addColumn);
                }
                page++; //完成一個班級換下個sheet的畫製
            }

            prototype.Worksheets.RemoveAt(0); //都完成後刪除第一個範本sheet

            Dictionary<string, int> columnIndexTable = new Dictionary<string, int>(); //Excel欄位索引
            //標記欄位索引
            columnIndexTable.Add("座號", 0);
            columnIndexTable.Add("學號", 1);
            columnIndexTable.Add("姓名", 2);
            columnIndexTable.Add("嘉獎", 4);
            columnIndexTable.Add("小功", 5);
            columnIndexTable.Add("大功", 6);
            columnIndexTable.Add("警告", 7);
            columnIndexTable.Add("小過", 8);
            columnIndexTable.Add("大過", 9);
            columnIndexTable.Add("累嘉獎", 10);
            columnIndexTable.Add("累小功", 11);
            columnIndexTable.Add("累大功", 12);
            columnIndexTable.Add("累警告", 13);
            columnIndexTable.Add("累小過", 14);
            columnIndexTable.Add("累大過", 15);
            columnIndexTable.Add("留查", 16);
            columnIndexTable.Add("輔安", 17);
            columnIndexTable.Add("全勤", 18);

            int index = 19;
            //標記新增的假別項目欄位索引
            foreach (String str in DisplayList)
            {
                columnIndexTable.Add(str, index);
                index++;
            }

            #endregion

            //填入表格
            BGW.ReportProgress(40, "填入表格");
            #region 填入表格
            _WK = new Workbook();
            int sheetIndex = 0;
            _WK.Copy(prototype); //複製畫製好欄位的範本
            Worksheet ws;
            Cells cs;

            //取得功過換算比例
            MeritDemeritReduceRecord mdrr = MeritDemeritReduce.Select();
            int? MAB = mdrr.MeritAToMeritB;
            int? MBC = mdrr.MeritBToMeritC;
            int? DAB = mdrr.DemeritAToDemeritB;
            int? DBC = mdrr.DemeritBToDemeritC;

            float progress = 50;
            float rate = (float)(100 - progress) / totalStudent; //進度百分比計算
            
            foreach (String classid in classDic.Keys)
            {
                ws = _WK.Worksheets[sheetIndex];
                cs = ws.Cells;
                //列印標題項目,曠課..病假..喪假...etc
                foreach (String type in DisplayList)
                {
                    cs[1, columnIndexTable[type]].PutValue(type);
                }

                index = 2; //列印起始索引
                _CountAllColumnValue = new Dictionary<int, int>(); //重制個項目的總數
                foreach (StudentRecord student in classDic[classid])
                {
                    progress += rate;
                    BGW.ReportProgress((int)progress, "正在填入資料...");
                    String id = student.ID;
                    int? 嘉獎 = MeritDemeritAttDic[id].MeritCCount;
                    int? 小功 = MeritDemeritAttDic[id].MeritBCount;
                    int? 大功 = MeritDemeritAttDic[id].MeritACount;
                    int? 警告 = MeritDemeritAttDic[id].DemeritCCount;
                    int? 小過 = MeritDemeritAttDic[id].DemeritBCount;
                    int? 大過 = MeritDemeritAttDic[id].DemeritACount;

                    //將功過轉為嘉獎和警告,做功過相抵計算
                    嘉獎 = 大功 * MAB * MBC + 小功 * MBC + 嘉獎;
                    警告 = 大過 * DAB * DBC + 小過 * DBC + 警告;

                    int?[] i = 功過相抵(嘉獎, 警告);
                    嘉獎 = i[0];
                    警告 = i[1];

                    //獎勵換算
                    int? 累嘉獎 = 嘉獎 % MBC;
                    int? 累小功 = (嘉獎 / MBC) % MAB;
                    int? 累大功 = (嘉獎 / MBC) / MAB;
                    //懲戒換算
                    int? 累警告 = 警告 % DBC;
                    int? 累小過 = (警告 / DBC) % DAB;
                    int? 累大過 = (警告 / DBC) / DAB;

                    cs[index, columnIndexTable["座號"]].PutValue(student.SeatNo);
                    cs[index, columnIndexTable["學號"]].PutValue(student.StudentNumber);
                    cs[index, columnIndexTable["姓名"]].PutValue(student.Name);

                    SetColumnValue(cs[index, columnIndexTable["嘉獎"]], MeritDemeritAttDic[id].MeritCCount);
                    SetColumnValue(cs[index, columnIndexTable["小功"]], MeritDemeritAttDic[id].MeritBCount);
                    SetColumnValue(cs[index, columnIndexTable["大功"]], MeritDemeritAttDic[id].MeritACount);
                    SetColumnValue(cs[index, columnIndexTable["警告"]], MeritDemeritAttDic[id].DemeritCCount);
                    SetColumnValue(cs[index, columnIndexTable["小過"]], MeritDemeritAttDic[id].DemeritBCount);
                    SetColumnValue(cs[index, columnIndexTable["大過"]], MeritDemeritAttDic[id].DemeritACount);
                    SetColumnValue(cs[index, columnIndexTable["累嘉獎"]], 累嘉獎);
                    SetColumnValue(cs[index, columnIndexTable["累小功"]], 累小功);
                    SetColumnValue(cs[index, columnIndexTable["累大功"]], 累大功);
                    SetColumnValue(cs[index, columnIndexTable["累警告"]], 累警告);
                    SetColumnValue(cs[index, columnIndexTable["累小過"]], 累小過);
                    SetColumnValue(cs[index, columnIndexTable["累大過"]], 累大過);
                    SetColumnValue(cs[index, columnIndexTable["留查"]], MeritDemeritAttDic[id].Flag ? "是" : "");
                    SetColumnValue(cs[index, columnIndexTable["全勤"]], MeritDemeritAttDic[id].全勤 ? "是" : "");

                    foreach (String type in DisplayList)  //列印勾選的假別
                    {
                        if (MeritDemeritAttDic[id].Attendance.ContainsKey(type))
                        {
                            SetColumnValue(cs[index, columnIndexTable[type]], MeritDemeritAttDic[id].Attendance[type]);
                        }
                    }
                    index++; //換下一列
                }

                //最後總計
                index = FixIndex(index);
                Range endRow = cs.CreateRange(0, 1, false);
                cs.CreateRange(index, 1, false).Copy(endRow);
                cs[index, 0].PutValue("總計");
                foreach (int cloumnIndex in _CountAllColumnValue.Keys)
                {
                    cs[index, cloumnIndex].PutValue(_CountAllColumnValue[cloumnIndex]);
                }

                //列印日期及學校班級資訊
                cs[0, 0].PutValue("列印日期:" + DateTime.Today.ToShortDateString());
                cs.CreateRange(0, 3, 1, columnIndexTable.Last().Value - 2).Merge(); //合併標題欄位的儲存格

                String title = String.Format("{0} {1} 學年度 {2} 學期 {3} 缺曠獎懲總表", K12.Data.School.ChineseName, _Schoolyear, _Semester == 1 ? "上" : "下", GetClassName(classid));

                cs[0, 3].PutValue(title);
                cs[0, 3].Style.Font.Size = 20; //設定標題文字大小
                sheetIndex++; //換下一個sheet(下一個班級班)
            }

            BGW.ReportProgress(100, "已完成 班級缺曠獎懲總表");

            #endregion
        }

        private void SetColumnValue(Cell cell, int? value) //輸出欄位的值並加總整個Column
        {
            cell.PutValue(value);
            if (!_CountAllColumnValue.ContainsKey(cell.Column))
            {
                _CountAllColumnValue.Add(cell.Column, 0);
            }
            if (value != null) //值不為空才做轉型
                _CountAllColumnValue[cell.Column] += (int)value;
        }

        private void SetColumnValue(Cell cell, String value) //輸出欄位的值並加總整個Column
        {
            cell.PutValue(value);
            if (!_CountAllColumnValue.ContainsKey(cell.Column))
            {
                _CountAllColumnValue.Add(cell.Column, 0);
            }
            if (value == "是")
                _CountAllColumnValue[cell.Column]++;
        }

        private int SortStudent(StudentRecord x, StudentRecord y)
        {
            int xx = x.SeatNo.HasValue ? x.SeatNo.Value : 0;
            int yy = y.SeatNo.HasValue ? y.SeatNo.Value : 0;
            return xx.CompareTo(yy);
        }

        private void buttonX2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private int?[] 功過相抵(int? Merit, int? Demerit)
        {
            int?[] i = new int?[2];
            if (Merit > Demerit)
            {
                Merit = Merit - Demerit;
                Demerit = 0;
            }
            else if (Merit < Demerit)
            {
                Demerit = Demerit - Merit;
                Merit = 0;
            }
            else
            {
                Merit = 0;
                Demerit = 0;
            }

            i[0] = Merit;
            i[1] = Demerit;

            return i;
        }

        //修正Excel輸出的索引值,補滿五個row為一單位
        private int FixIndex(int index)
        {
            int temp = (index - 2) % 5;
            if (temp != 0)
            {
                int add = 0;
                for (int i = temp; i < 5; i++)
                {
                    add++;
                }
                index += add;
            }
            return index;
        }

        //紙張設定
        private int GetSizeIndex()
        {
            Campus.Configuration.ConfigData cd = Campus.Configuration.Config.User[ConfigPrint];
            string config = cd["紙張設定"];
            int x = 0;
            int.TryParse(config, out x);
            return x; //如果是數值就回傳,如果不是回傳預設
        }

        //取得需列印的項目清單
        private List<String> GetUserType()
        {
            List<String> displayList = new List<string>();
            Campus.Configuration.ConfigData cd = Campus.Configuration.Config.User[ConfigType];
            String config = cd["假別設定"];

            if (!string.IsNullOrEmpty(config))
            {
                try
                {
                    XmlElement print = DSXmlHelper.LoadXml(config);

                    foreach (XmlElement elem in print.SelectNodes("//Type"))
                    {
                        String text = elem.GetAttribute("Text");
                        displayList.Add(text);
                    }
                }
                catch
                {
                    MessageBox.Show("取得假別設定失敗,請重新確認假別設定");
                }

            }
            return displayList;
        }

        //取得班級Name
        private String GetClassName(String id)
        {
            try
            {
                return _ClassNameDic[id];
            }
            catch
            {
                return "";
            }
        }

        //鎖定畫面元件
        private void FormControl(bool value)
        {
            intSchoolYear.Enabled = value;
            intSemester.Enabled = value;
            linkLabel1.Enabled = value;
            linkLabel2.Enabled = value;
            buttonX1.Enabled = value;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SelectPrintSizeForm sizeform = new SelectPrintSizeForm(ConfigPrint);
            sizeform.ShowDialog();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SelectTypeForm typeform = new SelectTypeForm(ConfigType);
            typeform.ShowDialog();
        }
    }
}