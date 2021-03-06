﻿using System;
using System.Windows.Forms;
using ExportSLDPRTToDXF.Models;
using ExportSLDPRTToDXF.Models.ORM;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using EPDM.Interop.EPDMResultCode;
using Patterns.Observer;
using System.Deployment.Application;
using ExportToXMLLib;
using SolidWorksLibrary.Builders.Dxf;

namespace ExportSLDPRTToDXF
{
    public partial class DataForm : Form
    {
        #region fields

        /// <summary>
        /// The FileModelPdm exemplar which contains 
        /// the main data about file in the PDM
        /// </summary>
        FileModelPdm FileModelPdm;

        internal static Properties.Settings settings { get { /*Properties.Settings.Default.Reload( ); */return Properties.Settings.Default; } }
        private string tempAppFolder { get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ExportPartToDXF"; } }

        IEnumerable<Specification> specificationsQuery;


        #endregion fields

        public DataForm()
        {
            Logger.Instance.RootDirectory = tempAppFolder;

            InitializeComponent();
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                Version version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                VersionLabel.Text = $"v. {version.ToString()}";
            }

            try
            {
                SolidWorksPdmAdapter.Instance.BoomId = settings.BoomId;
            }
            catch (Exception ex)
            {
                MessageObserver.Instance.SetMessage(ex.ToString());
                MessageBox.Show(ex.ToString());
            }
            PdmLogin();
        }

        public void PdmLogin()
        {
            try
            {
                if (!String.IsNullOrEmpty(settings.VaultName))

                {
                    SolidWorksPdmAdapter.Instance.AuthoLogin(settings.VaultName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не верное имя PDM вида: " + settings.VaultName + "\n" + ex.Message);
            }
        }

        private void Settings_btn_Click(object sender, EventArgs e)
        {
            SettingsForm settings_frm = new SettingsForm();
            settings_frm.ShowDialog();
        }

        /// <summary>
        /// Обработчик кнокп поиска
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_btn_Click(object sender, EventArgs e)
        {
            StatusLabel.Text = $"Статус: Начат поиск по запросу {Search_textBox.Text}";
            ClearAllControllers();
            SpecificationDataGridClear();
            try
            {
                var Resaltlist = SolidWorksPdmAdapter.Instance.SearchDoc("%" + Search_textBox.Text + "%");
                foreach (var item in Resaltlist)
                {
                    SearchResultList.Items.Add(item);
                }
                StatusLabel.Text = $"Статус: Поиск завершен. Найдено {Resaltlist.Count()} элемент(ов)";
            }
            catch (COMException ex)
            {
                MessageBox.Show("Error: " + (EdmResultErrorCodes_e)ex.ErrorCode);
            }
        }


        /// <summary>
        /// Occurs when selected eny item in the SearchResultList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchResultList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConfigurationsComboBoxClear();
            FileModelPdm = SearchResultList.SelectedItem as FileModelPdm;
            if (FileModelPdm != null)
            {
                ConfigurationsComboBox.Items.AddRange(SolidWorksPdmAdapter.Instance.GetConfigigurations(FileModelPdm.Path));

                if (ConfigurationsComboBox.Items.Count > 0)
                {
                    ConfigurationsComboBox.Text = (string)ConfigurationsComboBox.Items[0];
                }
            }
        }


        /// <summary>
        /// Occurs when selected any item in the ConfigurationsComboBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigurationsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //SpecificationDataGrid.Rows.Clear( );
            SpecificationDataGrid.DataSource = null;
            FillDataGridSpecification(ConfigurationsComboBox.SelectedItem.ToString());
        }

        Specification[] specifications;

        /// <summary>
        /// Fill data grid specification
        /// </summary>
        /// <param name="configuration"></param>
        private void FillDataGridSpecification(string configuration)
        {
            SpecificationDataGridClear();
            specifications = GetSpecification(FileModelPdm.Path, configuration);
            SpecificationDataGrid.DataSource = Specification.ConvertToViews(specifications);   // convert specification list to view spec. list which containe view name
                                                                                               // for each properties
        }

        /// <summary>
        /// Returns specification by each parts. 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal Specification[] GetSpecification(string filePath, string configuration)
        {
            try
            {
                var parts = AdapterPdmDB.Instance.Parts;
                var bomShell = SolidWorksPdmAdapter.Instance.GetBomShell(filePath, configuration);
                if (bomShell != null)
                {

                     specificationsQuery = (from eachBom in bomShell
                                      join eachPart in parts on new { id = eachBom.IdPdm, ver = eachBom.Version, conf = eachBom.Configuration }
                                      equals new { id = (int)eachPart.IDPDM, ver = eachPart.Version, conf = eachPart.ConfigurationName }
                                      into Spec_s

                                      from spec in Spec_s.DefaultIfEmpty()
                                      select new Specification
                                      {
                                          Description = eachBom.Description,
                                          PartNumber = eachBom.PartNumber,
                                          Version = eachBom.Version,
                                          Configuration = eachBom.Configuration,
                                          IDPDM = eachBom.IdPdm,
                                          Partition = eachBom.Partition,
                                          
                                          Bend = (spec == null ? 0 : spec.Bend),
                                          PaintX = (spec == null ? 0 : spec.PaintX),
                                          PaintY = (spec == null ? 0 : spec.PaintY),
                                          PaintZ = (spec == null ? 0 : spec.PaintZ),
                                          WorkpieceX = (spec == null ? 0 : spec.WorkpieceX),
                                          WorkpieceY = (spec == null ? 0 : spec.WorkpieceY),
                                          SurfaceArea = (spec == null ? 0 : spec.SurfaceArea),
                                          isDxf = (spec == null || spec.DXF != "1") ? false : true,
                                          FileName = eachBom.FileName,
                                          FilePath = Path.Combine(eachBom.FolderPath, eachBom.FileName),
                                          Thickness = (spec == null ? 0 : spec.Thickness)
                                      });

                    return specificationsQuery.ToList().Where(each => each.FileName.ToLower().Contains(".sldprt")
                                    && (each.Partition == string.Empty || each.Partition == "Детали")).ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return null;
        }


        #region GUI controllers clear region

        void SearchResultListClear()
        {
            SearchResultList.Items.Clear();
        }

        void SpecificationDataGridClear()
        {
            SpecificationDataGrid.DataSource = null;
        }

        void ConfigurationsComboBoxClear()
        {
            ConfigurationsComboBox.Items.Clear();
        }

        void ClearAllControllers()
        {
            SearchResultListClear();
            SpecificationDataGridClear();
            ConfigurationsComboBoxClear();
        }
        #endregion GUI controllers clear region

        /// <summary>
        /// Запуск построения DXF
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpLoadDxfButton_Click(object sender, EventArgs e)
        {
            DxfBulder DxfBulder = DxfBulder.Instance;
            string tempDxfFolder = tempAppFolder;
            int countIterations = 0;
            if (String.IsNullOrEmpty(settings.DxfPath))
            {
                MessageBox.Show("Не указан путь для выгрузки DXF");
            }
            else
            {
                tempDxfFolder = Path.Combine(tempDxfFolder, "DXF");

                DxfBulder.DxfFolder = tempDxfFolder;
                DxfBulder.FinishedBuilding += DxfBulder_FinishedBuilding;

                StatusLabel.Text = "Статус: Получение файлов";
                SolidWorksPdmAdapter.Instance.DownLoadFile(specifications);
                IEnumerable<FileModelPdm> fileModelsOfSpecification = Specification.ConvertToFileModels(specifications);

                StatusLabel.Text = "Статус: Выгрузка DXF файлов";
                
                if (specifications != null)
                {
                    specifications = specifications.GroupBy(each => each.FilePath).Select(each => new Specification
                    {
                        Description = each.First().Description,
                        PartNumber = each.First().PartNumber,
                        Version = each.First().Version,
                        Configuration = each.First().Configuration,
                        IDPDM = each.First().IDPDM,
                        Bend = each.First().Bend,
                        PaintX = each.First().PaintX,
                        PaintY = each.First().PaintY,
                        WorkpieceX = each.First().WorkpieceX,
                        PaintZ = each.First().PaintZ,
                        WorkpieceY = each.First().WorkpieceY,
                        SurfaceArea = each.First().SurfaceArea,
                        isDxf = each.First().isDxf,
                        FileName = each.First().FileName,
                        FilePath = each.First().FilePath,
                        Thickness = each.First().Thickness,
                    }).ToArray();


                    foreach (var eachSpec in specifications)
                    {
                        //ВНС-901.115.176

                        if (!eachSpec.isDxf && File.Exists(eachSpec.FilePath) && Path.GetExtension(eachSpec.FileName).ToUpper() == ".SLDPRT")
                        {
                            StatusLabel.Text = "Статус: Выгрузка DXF файла " + eachSpec.FileName + "-" + eachSpec.Configuration;
                        
                            DxfBulder.Build(eachSpec.FilePath, eachSpec.IDPDM, eachSpec.Version, eachSpec.Configuration);
                            countIterations++;
                        }
                    }
                }
            }
            SpecificationDataGrid.DataSource = Specification.ConvertToViews(specifications);
            MessageObserver.Instance.SetMessage($"Created {countIterations} new dxf files to temp folder");
            countIterations = 0;

            #region  load dxf as binary from database  and save as dxf file
            foreach (var item in specificationsQuery)
            {
                try
                {
                    if (AdapterPdmDB.Instance.IsDxf(item.IDPDM, item.Configuration, item.Version))
                    {
                        byte[] binary = AdapterPdmDB.Instance.GetDXF(item.IDPDM, item.Configuration, item.Version);
                        string fileName = Path.GetFileNameWithoutExtension(item.FileName).Replace("ВНС-", "");

                        
                        fileName = DXF.DxfNameBuild(fileName, item.Configuration) + "-" + item.Thickness.ToString().Replace(",", ".");

                        if (Path.GetExtension(FileModelPdm.FileName.ToUpper()) == ".SLDPRT")
                        {
                            BinaryToDxfFile(binary, fileName, Path.Combine(settings.DxfPath, "Детали"));
                        }
                        else
                        {
                            BinaryToDxfFile(binary, fileName, Path.Combine(settings.DxfPath, Path.GetFileNameWithoutExtension(FileModelPdm.FileName)));
                        }
                        countIterations++;
                    }
                }
                catch (Exception ex)
                {
                    MessageObserver.Instance.SetMessage($"Failed to save new dxf files to destination folder.\t" + item.FilePath + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                    throw ex;
                }
            }
            MessageObserver.Instance.SetMessage($"Save {countIterations} new dxf files to destination folder");
            #endregion

            #region clear temp dxf directory
            var files = Directory.GetFiles(tempDxfFolder);
            foreach (var file in files)
            {
                File.Delete(file);
            }
            #endregion
            try
            {
                StatusLabel.Text = "Статус: Выгрузка XML файлов";
                Export export = new Export(FileModelPdm.Path);
                export.XML();
                MessageObserver.Instance.SetMessage("XML noEX: ");
            }
            catch (Exception ex)
            {
                MessageObserver.Instance.SetMessage("XML EX: " + ex.StackTrace);
                MessageBox.Show("Failed to upload XML with exception: " + Environment.NewLine + ex.Message);
            }
            StatusLabel.Text = "Статус: Выгрузка DXF файлов завершена. Количество выгруженых файлов " + countIterations;
            countIterations = 0;
            SolidWorksPdmAdapter.Instance.KillProcsses("SLDWORKS");
            MessageObserver.Instance.SetMessage("End upload.\n");
        }

        private void BinaryToDxfFile(byte[] inputBinary, string fileName, string directory)
        {
            
            string path = DXF.DxfNameBuild(fileName, "");
            try
            {
                path = Path.Combine(directory, $"{fileName}.dxf");
            }
            catch (Exception  ex)
            {
                MessageObserver.Instance.SetMessage("BinaryToDxfFile failed to create path.\nfileName " + fileName +";    directory: " + directory);
                throw ex;
            }

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, inputBinary);
        }

        /// <summary>
        /// Выгрузка CutList для каждого построеного dxf
        /// </summary>
        /// <param name="dataToExport"></param>
        private void DxfBulder_FinishedBuilding(SolidWorksLibrary.Builders.Dxf.DataToExport dataToExport)
        {
            try
            {
                AdapterPdmDB.Instance.UpDateCutList(dataToExport.Configuration,
                    dataToExport.DXFByte,
                    dataToExport.WorkpieceX,
                    dataToExport.WorkpieceY,
                    dataToExport.Bend,
                    dataToExport.Thickness,
                    dataToExport.Version,
                    dataToExport.PaintX,
                    dataToExport.PaintY,
                    dataToExport.PaintZ,
                    dataToExport.IdPdm,
                    dataToExport.SurfaceArea,
                    dataToExport.MaterialID
                    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Search_textBox_MouseEnter(object sender, EventArgs e)
        {
            ToolTip.SetToolTip(Search_textBox, "Поле поиска");
        }

        private void ConfigurationsComboBox_MouseEnter(object sender, EventArgs e)
        {
            ToolTip.SetToolTip(ConfigurationsComboBox, "Список конфигураций");
        }

        private void Search_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && Search_textBox.Text != null)
            {
                Search_btn_Click(sender, null);
            }
        }
    }
}