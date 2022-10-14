using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DolbyDigitalSerialParser
{
    class Program
    {
        static void Main(string[] args)
        {

            
            int dataBitPos = 3;
            int clockBitPos = 7;
            int blockBitPos = 0;

            bool blockIndexKnown = false;
            int blockIndex = 0;
            int blockLength = 16;

            UInt16 currentBlock = 0;

            bool dataBit, clockBit, blockBit, lastBlockBit=false, lastClockBit = false;

            Int64 filePosition = -1;
            Int64 dataPosition = 0;

            StringBuilder textform = new StringBuilder();

            List<byte> output = new List<byte>();

            using (FileStream fs = new FileStream("binary.bin", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using(BinaryReader br = new BinaryReader(fs))
                {

                    byte[] tmpData = br.ReadBytes(1024 * 1024 * 10); // 10 MB is always read into RAM for processing. Faster than reading byte by byte I think.

                    int delay = 0;
                    bool readOutstanding = false;

                    foreach(byte tmpByte in tmpData)
                    {
                        filePosition++;
                        // This is to jump 2 samples forward when a change happens so that all channels are in sync, just in case.
                        if (delay > 0)
                        {
                            delay--;
                            continue;
                        }

                        dataBit = 1 == (tmpByte >> dataBitPos & 0b0000_0001);
                        clockBit = 1 == (tmpByte >> clockBitPos & 0b0000_0001);
                        blockBit = 1 == (tmpByte >> blockBitPos & 0b0000_0001);

                        // if clock signal changed, jump 2 forward
                        if (lastClockBit != clockBit && clockBit == true)
                        {
                            lastClockBit = clockBit;
                            delay = 1;
                            readOutstanding = true;
                            continue;
                        }

                        if (readOutstanding)
                        {

                            readOutstanding = false;

                            //if(blockBit == true) // start new block
                            if (lastBlockBit == true) // start new block
                            {
                                blockIndex = 0;
                                blockIndexKnown = true;
                                byte[] dataBlock = BitConverter.GetBytes(currentBlock);
                                //Array.Reverse(dataBlock);
                                output.AddRange(dataBlock);
                                currentBlock = 0;
                            } else
                            {
                                //if(blockBit == true)
                                //{
                                 //   blockIndex = -1;
                                //    blockIndexKnown = true;
                                //} else
                                if (!blockIndexKnown)
                                {
                                    lastBlockBit = blockBit;
                                    continue;
                                }
                                blockIndex++;
                            }

                            if (dataBit)
                            {
                                currentBlock = (UInt16)(currentBlock | (1<< (blockLength-blockIndex-1)));
                                textform.Append("1");
                            } else
                            {
                                textform.Append("0");

                            }

                            //readOutstanding = false;
                            lastBlockBit = blockBit;
                        }

                        
                        lastClockBit = clockBit;
                    }
                }
            }

            File.WriteAllBytes("binaryDecoded.ac3",output.ToArray());
            File.WriteAllText("binaryDecoded.txt",textform.ToString());

        }
    }
}
