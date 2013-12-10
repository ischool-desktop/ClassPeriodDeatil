using FISCA;
using FISCA.Presentation;
using FISCA.Permission;

namespace ClassPeriodDetail
{
    public class Program
    {
        [MainMethod()]
        public static void Main()
        {
            RibbonBarItem ClassReports = K12.Presentation.NLDPanels.Class.RibbonBarItems["資料統計"];
            ClassReports["報表"]["學務相關報表"]["班級缺曠獎懲總表(含功過相抵)"].Click += delegate
            {
                MainForm calc = new MainForm();
                calc.ShowDialog();
            };

            K12.Presentation.NLDPanels.Class.SelectedSourceChanged += delegate
            {
                ClassReports["報表"]["學務相關報表"]["班級缺曠獎懲總表(含功過相抵)"].Enable = (K12.Presentation.NLDPanels.Class.SelectedSource.Count > 0) && Permissions.班級缺曠獎懲總表權限;
            };

            Catalog ribbon = RoleAclSource.Instance["班級"]["報表"];
            ribbon.Add(new RibbonFeature(Permissions.班級缺曠獎懲總表, "班級缺曠獎懲總表(含功過相抵)"));

        }
    }
}
