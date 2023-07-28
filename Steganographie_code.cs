﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;


namespace WindowsFormsApp1
{
    public partial class Stego : Form
    {
        public Stego()
        {
            InitializeComponent();
        }

        private void butladeneins_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "Bitmap (*.bmp)|*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textPathBitmap.Text = ofd.FileName;
                textNameBitmap.Text = ofd.SafeFileName;
            }
        }

        private void butladenzwei_Click(object sender, EventArgs e)
        {
            OpenFileDialog dfo = new OpenFileDialog();

            if (dfo.ShowDialog() == DialogResult.OK)
            {
                textPathEncrypt.Text = dfo.FileName;
                textNameEncrypt.Text = dfo.SafeFileName;
            }
        }

        private void butschluess_Click(object sender, EventArgs e)
        {
            if (radioEncrypt.Checked == true)
            {
                if (textNameBitmap.Text != "" && textNameEncrypt.Text != "")
                {
                    int sourceCompressed;
                    int sourceSize;
                    int sourceDataSize;
                    int sourceHeaderEnd;
                    int sourceColorDepth;
                    string sourcePath = textPathBitmap.Text;
                    byte[] sourceData;
                    BinaryReader sourceReader;

                    long dataLength;
                    string dataPath = textPathEncrypt.Text;
                    string dataName;
                    byte[] data;


                    int targetCount;
                    string targetPath;
                    byte[] targetHeader;
                    byte[] targetData;


                    // Edit Path for the new File
                    dataName = dataPath.Split('\\').Last();
                    targetPath = Path.ChangeExtension(sourcePath, "_.bmp");


                    // Read and Save Bitmap Infos
                    var BitmapInfo = new FileInfo(textPathBitmap.Text);
                    var BitmapStream = BitmapInfo.Open(FileMode.Open, FileAccess.Read);

                    sourceReader = new BinaryReader(BitmapStream);

                    sourceReader.BaseStream.Position = 2;
                    sourceSize = sourceReader.ReadInt32();
                    
                    sourceReader.BaseStream.Position = 10;
                    sourceHeaderEnd = sourceReader.ReadInt32();

                    sourceReader.BaseStream.Position = 28;
                    sourceColorDepth = sourceReader.ReadInt16();

                    sourceCompressed = sourceReader.ReadInt32();

                    sourceDataSize = sourceReader.ReadInt32();

                    BitmapStream.Close();



                    if (sourceCompressed == 0)
                    {

                        if (sourceColorDepth >= 24)
                        {
                            // Read all Bytes from the Bitmap-File
                            FileStream sourceFilestream = new System.IO.FileStream(textPathBitmap.Text, System.IO.FileMode.Open);
                            sourceData = imageToByteArray(sourceFilestream);
                            sourceFilestream.Close();
                        
                            targetData = new byte[sourceSize];


                            // Open the File to be encrypted
                            FileStream dataFile = new System.IO.FileStream(dataPath, System.IO.FileMode.Open);
                            FileInfo dataFi = new FileInfo(dataPath);
                            dataLength = dataFi.Length;


                            if ((dataLength + 9 + dataName.Length) * 8 <= sourceDataSize)
                            {
                                // Read all Bytes from the File to be encrypted
                                data = imageToByteArray(dataFile);

                                dataFile.Close();


                                // Copy the Bitmap-Header from the Source to the Target
                                for (targetCount = 0; targetCount < sourceHeaderEnd; targetCount++)
                                {
                                    targetData[targetCount] = sourceData[targetCount];
                                }


                                // Creat a new specific Header
                                targetHeader = createHeader(dataLength, dataName);
                                encryptByte(targetHeader, sourceData, ref targetData, ref targetCount);


                                // Encrypt the Data of the File to be encrypted
                                encryptByte(data, sourceData, ref targetData, ref targetCount);


                                // Copy the remaining Filedata to the Target
                                for (int i = targetCount; i < sourceSize; i++)
                                {
                                    targetData[i] = sourceData[i];
                                }


                                // Wirte all Bytes to the new File and delete the original File
                                File.WriteAllBytes(targetPath, targetData);

                                File.Delete(textPathEncrypt.Text);

                                textMessages.Text = "Das Verschluesseln war erfolgreich. Die Datei liegt in " + targetPath;

                                textPathEncrypt.Clear();
                                textNameEncrypt.Clear();
                            }
                            else
                            {
                                dataFile.Close();

                                textMessages.Text = "Die zu verschluesselnde Datei ist zu groß.";
                            }

                            dataFile.Close();

                        }
                        else
                        {
                            textMessages.Text = "Die Farbtiefe der Bitmap Datei ist zu klein.";
                        }

                    }
                    else
                    {
                        textMessages.Text = "Die Bitmap Datei ist compressed.";
                    }

                }
                else
                {
                    textMessages.Text = "Mindestens eine Datei fehlt! Es muessen zwei Dateien geladen sein.";
                }

            }
            else
            {
                if (textNameBitmap.Text != "")
                {
                    int nameSize;
                    int byteSize;
                    string check;

                    int sourceCounter;
                    string sourcePath = textPathBitmap.Text;
                    string sourcePathLast;
                    byte[] sourceCheck;
                    byte[] sourceData;
                    BinaryReader sourceReader;


                    string targetPath;
                    string targetName;
                    byte[] targetData;
                    byte[] targetSize;
                    byte[] targetNameLength;
                    byte[] targetNameByte;



                    // Read and Save Bitmap Infos
                    var BitmapInfo = new FileInfo(sourcePath);
                    var BitmapStream = BitmapInfo.Open(FileMode.Open, FileAccess.Read);

                    sourceReader = new BinaryReader(BitmapStream);
                    
                    sourceReader.BaseStream.Position = 10;
                    sourceCounter = sourceReader.ReadInt32();
                    
                    BitmapStream.Close();


                    // Read all Bytes from the Bitmap-File
                    FileStream sourceFilestream = new System.IO.FileStream(sourcePath, System.IO.FileMode.Open);
                    sourceData = imageToByteArray(sourceFilestream);
                    sourceFilestream.Close();
                    

                    // Decrypt the Checkchar
                    sourceCheck = decryptByte(sourceData, ref sourceCounter, 1);
                    check = Encoding.ASCII.GetString(sourceCheck, 0, sourceCheck.Length);

                    if (check == "*")
                    {

                        targetSize = decryptByte(sourceData, ref sourceCounter, 4);
                        byteSize = BitConverter.ToInt32(targetSize, 0) / 8; 
                        
                        
                        // Decrypt the Filename an create an new Path
                        targetNameLength = decryptByte(sourceData, ref sourceCounter, 4);
                        nameSize = BitConverter.ToInt32(targetNameLength, 0);
                        targetNameByte = decryptByte(sourceData, ref sourceCounter, nameSize);
                        targetName = Encoding.ASCII.GetString(targetNameByte, 0, targetNameByte.Length);
                        
                        sourcePathLast = sourcePath.Split('\\').Last();
                        
                        targetPath = sourcePath.Substring(0, sourcePath.Length - sourcePathLast.Length);
                        targetPath += targetName;


                        // Decrypt all Data from the encrypted File
                        targetData = decryptByte(sourceData, ref sourceCounter, byteSize);


                        // Wirte all Bytes to the new File and delete the encrypted File
                        File.WriteAllBytes(targetPath, targetData);
                        File.Delete(sourcePath);

                        textMessages.Text = "Das Entschluesseln war erfolgreich. Die Datei liegt in " + targetPath;

                        textPathBitmap.Clear();
                        textNameBitmap.Clear();
                    }
                    else
                    {
                        BitmapStream.Close();

                        textMessages.Text = "Die Datei entspricht nicht dem vorgegebenen Format.";
                    }
                }
                else
                {
                    textMessages.Text = "Es ist keine Datei zum Entschluesseln angegeben.";
                }
            }
        }

        private void butClear_Click(object sender, EventArgs e)
        {
            textNameBitmap.Clear();
            textPathBitmap.Clear();
            textMessages.Clear();
            textNameEncrypt.Clear();
            textPathEncrypt.Clear();
        }

        private void radiover_CheckedChanged(object sender, EventArgs e)
        {
            butLoadEncrpt.Enabled = true;
            butCrypt.Text = "Verschluesseln";
        }

        private void radioent_CheckedChanged(object sender, EventArgs e)
        {
            butLoadEncrpt.Enabled = false;
            textNameEncrypt.Clear();
            textPathEncrypt.Clear();
            butCrypt.Text = "Entschluesseln";
        }


        private byte[] createHeader(long length, string name)
        {
            string check = "*";
            byte[] size;
            byte[] header = new byte[9 + name.Length];

            header[0] = Encoding.ASCII.GetBytes(check)[0];


            // Encrypt the Size of the to be encrypted Data
            size = BitConverter.GetBytes((int)length * 8);

            for (int i = 0; i < 4; i++)
            {
                header[i + 1] = size[i];
            }


            // Encrypt the Name of the to be encrypted File
            size = BitConverter.GetBytes(name.Length);

            for (int i = 0; i < 4; i++)
            {
                header[i + 5] = size[i];
            }
            
            for (int i = 0; i < name.Length; i++)
            {
                header[i + 9] = Encoding.ASCII.GetBytes(name.Substring(i, 1))[0];
            }
            
            return header;
        }

        private void encryptByte(byte[] source, byte[] data, ref byte[] target, ref int targetCounter)
        {
            int icounter = 0;
        
            byte[] sourceByte = new byte[1];

            for (int i = 0; i < source.Length; i++)
            {
                sourceByte[0] = source[i];
                BitArray sourceBit = new BitArray(sourceByte);

                encryptBit(ref data, ref target, sourceBit, ref targetCounter, ref icounter);
            }
        }

        private void encryptBit(ref byte[] data, ref byte[] target, BitArray sourceBit, ref int targetCounter, ref int icounter)
        {
            byte[] targetByte = new byte[1];

            for (int j = 0; j <= 7; j++)
            {
                targetByte[0] = data[targetCounter];
                BitArray targetBit = new BitArray(targetByte);

                targetBit[0] = sourceBit[j];
                targetBit.CopyTo(target, targetCounter);

                icounter++;
                targetCounter++;
            }
        }

        private byte[] decryptByte(byte[] sourceData, ref int sourceCounter, int length)
        {
            byte[] target = new byte[length];
            
            for (int i = 0; i < length; i++)
            {
                byte[] sourceByte = new byte[1];
                BitArray targetBit = new BitArray(8);
    
                for (int j = 0; j < 8; j++)
                {
                    sourceByte[0] = sourceData[sourceCounter];
                    targetBit[j] = new BitArray(sourceByte)[0];
    
                    sourceCounter++;
                }
    
                targetBit.CopyTo(target, i);
            }

            return target;
        }

        public byte[] imageToByteArray(FileStream file)
        {
            // Read all Bytes from a Filestream
            byte[] imageArray;
            BinaryReader br = new BinaryReader(file);
            imageArray = br.ReadBytes((int)file.Length);
            return imageArray;
        }
    }
}
