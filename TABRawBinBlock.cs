using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;

namespace MapInfo.IO
{
    /// <summary>
    /// This is the base class used for all other data block types...&#13;
    /// it contains all the base functions to handle binary data.&#10;
    /// Это базовый класс для всех остальных типов блоков данных ... 
    /// Он содержит все базовые функции для обработки двоичных данных.
    /// </summary>
    internal class TABRawBinBlock
    {
        #region
        /// <summary>
        /// Связанный дескриптор файла
        /// </summary>
        public Stream m_fp; // Associated file handle

        protected long Position
        {
            get { return m_fp.Position; }
            set { m_fp.Position = value; }
        }

        protected TABAccess m_eAccess = new TABAccess(); // Read/Write access mode 

        protected SupportedBlockTypes m_nBlockType;
        /// <summary>
        /// Буфер содержит данные блоков
        /// </summary>
        protected byte m_pabyBuf; // Buffer to contain the block's data 
        /// <summary>
        /// Размер текущего блока (и буфера)
        /// </summary>
        protected int m_nBlockSize; // Size of current block (and buffer) 
        /// <summary>
        /// Количество байтов, используемых в буфере
        /// </summary>
        protected int m_nSizeUsed; // Number of bytes used in buffer 
        /// <summary>
        /// TRUE=Блоки должны быть всегда nSize байт
        /// FALSE=последний блок может быть меньше, чем nSize
        /// </summary>
        protected bool m_bHardBlockSize = new bool();
        /// <summary>
        /// Расположение текущего блока в файле
        /// </summary>
        protected int m_nFileOffset; // Location of current block in the file 
        /// <summary>
        /// Следующий байт для чтения из m_pabyBuf []
        /// </summary>
        protected int m_nCurPos; // Next byte to read from m_pabyBuf[] 
        /// <summary>
        /// Размер заголовка файла, если отличается от размера блока (используется GotoByteInFile ())
        /// </summary>
        protected int m_nFirstBlockPtr; // Size of file header when different from block size (used by GotoByteInFile())
        /// <summary>
        /// Используется только для обнаружения изменений
        /// </summary>
        protected bool m_bModified = false; // Used only to detect changes

        public Int32 GetStartAddress()
        {
            return m_nFileOffset;
        }

        public void SetModifiedFlag(bool bModified)
        {
            m_bModified = bModified;
        }

        /// <summary>
        /// This semi-private method gives a direct access to the internal buffer... /n
        /// to be used with extreme care!
        /// Это полу-частный метод дает прямой доступ к внутренним буфером, 
        /// который будет использоваться с особой осторожностью!
        /// </summary>
        /// <returns></returns>
        public int GetCurDataPtr()
        {
            return (m_pabyBuf + m_nCurPos);
        }
        #endregion

        public TABRawBinBlock() { }
        public TABRawBinBlock(Stream stream)
        {
            m_fp = stream;
        }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="eAccessMode"></param>
        /// <param name="bHardBlockSize"></param>
        public TABRawBinBlock(TABAccess eAccessMode, bool bHardBlockSize)
        {
            m_bHardBlockSize = bHardBlockSize;
            m_eAccess = eAccessMode;
            m_nFirstBlockPtr = 0;
            m_nBlockSize = m_nSizeUsed = m_nFileOffset = m_nCurPos = 0;

        }

        /// <summary>
        /// Load data from the specified file location and initialize the block.
        /// Загрузка данных из указанного местоположения файла и инициализировать блок.
        /// </summary>
        /// <param name="fpSrc"></param>
        /// <param name="nOffset"></param>
        /// <param name="nSize"></param>
        /// <returns>
        /// Returns 0 if succesful or -1 if an error happened, in which case CPLError() will have been called.
        /// Возвращает 0, если успешным или -1, если произошло ошибок, и в этом случае CPLError () будет были названы.
        /// </returns>
        private bool ReadFromFile(Stream fpSrc, int nOffset, int nSize /*= 512*/)
        {

            if (fpSrc == null || nSize == 0)
            {
                throw new Exception("Утверждение не удалось!"); //CPLError(CE_Failure, CPLE_AssertionFailed, );
                //return false;
            }

            m_fp = fpSrc;
            m_nFileOffset = nOffset;
            m_nCurPos = 0;
            m_bModified = false;

            //pabyBuf = (GByte)CPLMalloc(nSize * sizeof(GByte)); // Alloc a buffer to contain the data

            //    ----------------------------------------------------------------
            //     * Read from the file
            //     *---------------------------------------------------------------
            //if (VSIFSeek(fpSrc, nOffset, SEEK_SET) != 0 || (m_nSizeUsed = VSIFRead(pabyBuf, sizeof(GByte), nSize, fpSrc)) == 0 || (m_bHardBlockSize && m_nSizeUsed != nSize))
            //{
            //    CPLError(CE_Failure, CPLE_FileIO, "ReadFromFile() failed reading %d bytes at offset %d.", nSize, nOffset);
            //    CPLFree(pabyBuf);
            //    return -1;
            //}

            //    ---------------------------------------------------------------
            //     Init block with the data we just read
            //    ---------------------------------------------------------------
            return InitBlockFromData(fpSrc, nSize, m_nSizeUsed, nOffset);
        }

        /// <summary>
        /// Set the binary data buffer and initialize the block.
        /// Установите двоичный буфер данных и инициализируйте блок.
        /// Вызов ReadFromFile () автоматически вызывает InitBlockFromData () для завершения 
        /// инициализации блока после чтения данных из файла. Производные классы должны 
        /// осуществлять свою собственную версию InitBlockFromData (), если они нуждаются 
        /// в особой инициализации ... в этом случае происходит InitBlockFromData () должен вызвать 
        /// InitBlockFromData (), прежде чем делать что-нибудь еще.
        /// </summary>
        /// <param name="pabyBuf"></param>
        /// <param name="nBlockSize"></param>
        /// <param name="nSizeUsed"></param>
        /// <param name="bMakeCopy"></param>
        /// <param name="fpSrc"></param>
        /// <param name="nOffset"></param>
        /// <returns></returns>
        private bool InitBlockFromData(Stream fpSrc, int nBlockSize, int nSizeUsed, int nOffset)
        {
            m_fp = fpSrc;
            m_nFileOffset = nOffset;
            m_nCurPos = 0;
            //m_bModified = 0;

            //    ----------------------------------------------------------------
            //     * Alloc or realloc the buffer to contain the data if necessary
            //     *---------------------------------------------------------------
            //if (bMakeCopy == null)
            //{
            //    if (m_pabyBuf != null)
            //        CPLFree(m_pabyBuf);
            //    m_pabyBuf = pabyBuf;
            //    m_nBlockSize = nBlockSize;
            //    m_nSizeUsed = nSizeUsed;
            //}
            //else if (m_pabyBuf == null || nBlockSize != m_nBlockSize)
            //{
            //    //m_pabyBuf = (GByte)CPLRealloc(m_pabyBuf, nBlockSize * sizeof(GByte));
            //    m_nBlockSize = nBlockSize;
            //    m_nSizeUsed = nSizeUsed;
            //    //C++ TO C# CONVERTER TODO TASK: The memory management function 'memcpy' has no equivalent in C#:
            //    //memcpy(m_pabyBuf, pabyBuf, m_nSizeUsed);
            //}

            //    ----------------------------------------------------------------
            //     * Extract block type... header block (first block in a file) has
            //     * no block type, so we assign one by default.
            //     *---------------------------------------------------------------
            if (m_nFileOffset == 0)
                m_nBlockType = SupportedBlockTypes.TABMAP_HEADER_BLOCK;
            else
            {
                // Block type will be validated only if GetBlockType() is called
                // Тип блока будут проверяться только в том случае GetBlockType () вызывается
                Position = 0;
                m_nBlockType = (SupportedBlockTypes)Read(1)[0];
            }

            return true;
        }

        //*********************************************************************
        // *                   TABCreateMAPBlockFromFile()
        // *
        // * Load data from the specified file location and create and initialize 
        // * a TABMAP*Block of the right type to handle it.
        // *
        // * Returns the new object if succesful or NULL if an error happened, in 
        // * which case CPLError() will have been called.
        // *********************************************************************
        //        private TABRawBinBlock TABCreateMAPBlockFromFile(ref FILE fpSrc, int nOffset, int nSize, GBool bHardBlockSize, TABAccess eAccessMode)
        //{
        //    TABRawBinBlock poBlock = null;
        //    GByte pabyBuf;

        //    if (fpSrc == null || nSize == 0)
        //    {
        //        CPLError(CE_Failure, CPLE_AssertionFailed, "TABCreateMAPBlockFromFile(): Assertion Failed!");
        //        return null;
        //    }

        ////    ----------------------------------------------------------------
        ////     * Alloc a buffer to contain the data
        ////     *---------------------------------------------------------------
        //    pabyBuf = (GByte)CPLMalloc(nSize *sizeof(GByte));

        ////    ----------------------------------------------------------------
        ////     * Read from the file
        ////     *---------------------------------------------------------------
        //    if (VSIFSeek(fpSrc, nOffset, SEEK_SET) != 0 || VSIFRead(pabyBuf, sizeof(GByte), nSize, fpSrc)!=(uint)nSize)
        //    {
        //        CPLError(CE_Failure, CPLE_FileIO, "TABCreateMAPBlockFromFile() failed reading %d bytes at offset %d.", nSize, nOffset);
        //        CPLFree(pabyBuf);
        //        return null;
        //    }

        ////    ----------------------------------------------------------------
        ////     * Create an object of the right type
        ////     * Header block is different: it does not start with the object 
        ////     * type byte but it is always the first block in a file
        ////     *---------------------------------------------------------------
        //    if (nOffset == 0)
        //    {
        //        poBlock = new TABMAPHeaderBlock;
        //    }
        //    else
        //    {
        //        switch(pabyBuf[0])
        //        {
        //          case TABMAP_INDEX_BLOCK:
        //            poBlock = new TABMAPIndexBlock(eAccessMode);
        //            break;
        //          case TABMAP_OBJECT_BLOCK:
        //            poBlock = new TABMAPObjectBlock(eAccessMode);
        //            break;
        //          case TABMAP_COORD_BLOCK:
        //            poBlock = new TABMAPCoordBlock(eAccessMode);
        //            break;
        //          case TABMAP_TOOL_BLOCK:
        //            poBlock = new TABMAPToolBlock(eAccessMode);
        //            break;
        //          case TABMAP_GARB_BLOCK:
        //          default:
        //            poBlock = new TABRawBinBlock(eAccessMode, bHardBlockSize);
        //            break;
        //        }
        //    }

        ////    ----------------------------------------------------------------
        ////     * Init new object with the data we just read
        ////     *---------------------------------------------------------------
        //    if (poBlock.InitBlockFromData(pabyBuf, nSize, nSize, 0, fpSrc, nOffset) != 0)
        //    {
        //        // Some error happened... and CPLError() has been called
        //        poBlock = null;
        //        poBlock = null;
        //    }

        //    return poBlock;
        //}

        public SupportedBlockTypes GetBlockClass()
        {
            // Extract block type... header block (first block in a file) has no block type, so we assign one by default.
            // Экстракт блок типа ... Блок заголовка (первый блок в файле) не имеет тип блока, так что мы назначить его по умолчанию.
            if (m_fp == null)
                return SupportedBlockTypes.TAB_RAWBIN_BLOCK;
            else
            {
                // Block type will be validated only if GetBlockType() is called
                // Тип блока будут проверяться только в том случае GetBlockType () вызывается
                //Position = 0;
                return (SupportedBlockTypes)Read(1)[0];
            }

        }

        private byte[] read = new byte[8];
        protected byte[] Read(int count)
        {
            m_fp.Read(read, 0, count);
            return read;
        }
        protected void Read(ref byte var)
        {
            m_fp.Read(read, 0, 1);
            var = read[0];
        }
        protected void Read(ref short var)
        {
            m_fp.Read(read, 0, 2);
            var = BitConverter.ToInt16(read, 0);
        }
        protected void Read(ref int var)
        {
            m_fp.Read(read, 0, 4);
            var = BitConverter.ToInt32(read, 0);
        }
        protected void Read(ref long var)
        {
            m_fp.Read(read, 0, 8);
            var = BitConverter.ToInt64(read, 0);
        }
        protected void Read(ref double var)
        {
            m_fp.Read(read, 0, 8);
            var = BitConverter.ToDouble(read, 0);
        }
        static public void ReadVars(Stream m_fp, ref object var)
        {
            byte[] read = new byte[8];
            //for (int i = 0; i < list.Length; i++)
            //{
            if ((var) is byte)
            {
                m_fp.Read(read, 0, 1);
                var = read[0];
            }
            else if ((var) is short)
            {
                m_fp.Read(read, 0, 2);
                var = BitConverter.ToInt16(read, 0);
            }
            else if ((var) is int)
            {
                m_fp.Read(read, 0, 4);
                var = BitConverter.ToInt32(read, 0);
            }
            else if ((var) is long)
            {
                m_fp.Read(read, 0, 8);
                var = BitConverter.ToInt64(read, 0);
            }
            else if ((var) is double)
            {
                m_fp.Read(read, 0, 8);
                var = BitConverter.ToDouble(read, 0);
                //};
            }
        }
    }

    /// <summary>
    /// Режим доступа: чтение или запись,
    /// </summary>
    public enum TABAccess : int
    {
        TABRead,
        TABWrite,
        TABReadWrite // ReadWrite not implemented yet 
    }

    /// <summary>
    /// структура, что используется для хранения параметров проекции из заголовка .MAP
    /// </summary>
    public class TABProjInfo
    {
        public byte nProjId; // See MapInfo Ref. Manual, App. F and G
        public byte nEllipsoidId;
        public byte nUnitsId;
        /// <summary>
        /// params in same order as in .MIF COORDSYS
        /// </summary>
        public double[] adProjParams = new double[6];
        /// <summary>
        /// Datum Id added in MapInfo 7.8+ (.map V500)
        /// </summary>
        public short nDatumId = 0;
        /// <summary>
        /// Before that, we had to always lookup datum parameters to establish datum id
        /// </summary>
        public double dDatumShiftX;
        public double dDatumShiftY;
        public double dDatumShiftZ;
        public double[] adDatumParams = new double[5];

        /// <summary>
        /// Affine parameters only in .map version 500 and up
        /// <remarks>false=No affine param, true=Affine params</remarks>
        /// </summary>
        public bool nAffineFlag;
        public byte nAffineUnits;
        /// <summary>
        /// Affine params A-F
        /// </summary>
        public double[] dAffineParam = new double[6];
    }

    internal class TABRawBlock
    {
        public const short Size = 0x200;
        internal byte[] read; // = new byte[Size];
        internal List<byte[]> raws = new List<byte[]>();
        public short Position = 0;
        //public SupportedBlockTypes BlockClass = SupportedBlockTypes.TAB_RAWBIN_BLOCK;

        public static byte[] GetBlock(Stream stream)
        {
            byte[] block = new byte[Size];
            stream.Read(block, 0, Size);
            return block;
        }

        public static SupportedBlockTypes GetBlockClass(byte[] block)
        {
            if (block != null)
                return (SupportedBlockTypes)block[0];
            else
                return SupportedBlockTypes.TAB_RAWBIN_BLOCK;
        }

        public TABRawBlock() { }
        /// <summary>
        /// Читаем блок
        /// </summary>
        /// <param name="stream"></param>
        public TABRawBlock(byte[] block)
        {
            Add(block);
        }

        public virtual void Add(byte[] block)
        {
            read = block;
            raws.Add(read);
        }

        //public TABRawBlock(TABRawBlock blk)
        //{
        //    read = blk.read;
        //    Position = blk.Position;
        //    //BlockClass = blk.BlockClass;
        //}

        protected byte[] Read(short count)
        {
            byte[] var = new byte[count];
            //read.CopyTo(var, Position);
            Array.Copy(read, Position, var, 0, count);
            Position += count;
            return var;
        }
        protected void Read(ref byte var)
        {
            var = read[Position];
            Position += 1;
        }

        public byte ReadByte()
        {
            byte var = 0;
            Read(ref var);
            return var;
        }

        protected void Read(ref sbyte var)
        {
            var = (sbyte)read[Position];
            Position += 1;
        }

        public sbyte ReadSByte()
        {
            sbyte var = 0;
            Read(ref var);
            return var;
        }
        protected void Read(ref short var)
        {
            var = BitConverter.ToInt16(read, Position);
            Position += 2;
        }

        public short ReadInt16()
        {
            short var = 0;
            Read(ref var);
            return var;
        }
        protected void Read(ref int var)
        {
            var = BitConverter.ToInt32(read, Position);
            Position += 4;
        }

        public int ReadInt32()
        {
            int var = 0;
            Read(ref var);
            return var;
        }

        protected void Read(ref long var)
        {
            var = BitConverter.ToInt64(read, 8);
            Position += 8;
        }
        protected void Read(ref double var)
        {
            var = BitConverter.ToDouble(read, Position);
            Position += 8;
        }

        public double ReadDouble()
        {
            double var = 0;
            Read(ref var);
            return var;
        }

    }

    /// <summary>
    /// Supported .MAP block types (the first byte at the beginning of a block)
    /// Поддерживаемые типы блоков .MAP (первый байт в начале блока)
    /// </summary>
    enum SupportedBlockTypes : sbyte
    {
        /// <summary>
        /// TAB_RAWBIN_BLOCK = -1
        /// </summary>
        TAB_RAWBIN_BLOCK = -1,
        /// <summary>
        /// TABMAP_HEADER_BLOCK = 0
        /// </summary>
        TABMAP_HEADER_BLOCK = 0,
        TABMAP_INDEX_BLOCK = 1,
        TABMAP_OBJECT_BLOCK = 2,
        TABMAP_COORD_BLOCK = 3,
        TABMAP_GARB_BLOCK = 4,
        TABMAP_TOOL_BLOCK = 5,
        TABMAP_LAST_VALID_BLOCK_TYPE = 5
    }

    /// <summary>
    /// Общая информация о системной таблице и внутренней структуры координат
    /// </summary>
    internal class TABMAPHeaderBlock : TABRawBlock
    {
        #region Vars
        // Set various constants used in generating the header block.
        // Установите различные константы, используемые в создании блока заголовка.
        public const int HDR_MAGIC_COOKIE = 42424242;
        public const int HDR_VERSION_NUMBER = 500;
        //public const int HDR_DATA_BLOCK_SIZE = 512;
        public const byte HDR_DEF_ORG_QUADRANT = 1;   // N-E Quadrant
        public const int HDR_DEF_REFLECTXAXIS = 0;
        public const int HDR_OBJ_LEN_ARRAY_SIZE = 73;
        // The header block starts with an array of map object length constants.
        // Блок заголовка начинается с массива констант длины объекта карты.
        internal static byte[] gabyObjLenArray = { 
          0x00,0x0a,0x0e,0x15,0x0e,0x16,0x1b,0xa2, 
          0xa6,0xab,0x1a,0x2a,0x2f,0xa5,0xa9,0xb5, 
          0xa7,0xb5,0xd9,0x0f,0x17,0x23,0x13,0x1f, 
          0x2b,0x0f,0x17,0x23,0x4f,0x57,0x63,0x9c, 
          0xa4,0xa9,0xa0,0xa8,0xad,0xa4,0xa8,0xad, 
          0x16,0x1a,0x39,0x0d,0x11,0x37,0xa5,0xa9, 
          0xb5,0xa4,0xa8,0xad,0xb2,0xb6,0xdc,0xbd, 
          0xbd,0xf4,0x2b,0x2f,0x55,0xc8,0xcc,0xd8, 
          0xc7,0xcb,0xd0,0xd3,0xd7,0xfd,0xc2,0xc2, 
          0xf9};

        //0x0	1	1	Header Block identifier (Value: 0x0) [!]
        public Byte identifier;
        //0x1	1	1	Header Block header
        public Byte header;
        //:
        //:Unknown (For length of header data offset see 0x163)
        //:
        //0x33/0x2D/0x27/0x1F
        //:
        //: (Value 0x0 [!])
        //: 0xFF
        public byte[] Unknown = new byte[253];
        //0x100	4	1	Magic Number (0x28757B2 i.e.42424242) [?]
        public int MagicNumber;

        //    Установите допустимые значения по умолчанию для переменных.

        /// <summary>
        /// 0x104	2	1	Map File Version (not equal to table version)
        /// </summary>
        public short m_nMAPVersionNumber = HDR_VERSION_NUMBER;
        /// <summary>
        /// 0x106	2	1	Unknown value: 0x200 [!], BlockSize[??]
        /// </summary>
        short m_nBlockSize = Size;

        /// <summary>
        /// 0x108	8	1	CoordSysToDistUnits: Miles/LatDegree for Lat/Long maps 1.0  for all others [!]
        /// </summary>
        double m_dCoordsys2DistUnits = 1.0;
        /// <summary>
        /// 0x110	4	4	Coordinates of Minimum Bounding Rectangle (MBR)
        /// </summary>
        int m_nXMin = -1000000000;
        int m_nYMin = -1000000000;
        int m_nXMax = 1000000000;
        int m_nYMax = 1000000000;
        //m_bIntBoundsOverflow = FALSE;

        /// 0x120	4	4	Coordinates of Default View of table

        /// <summary>
        /// 0x130	4	1	Offset of Object Definition Index (see also 0x15F)
        /// </summary>
        int m_nFirstIndexBlock = 0;
        /// <summary>
        /// 0x134	4	1	Offset of the beginning of Deleted Block sequence
        /// </summary>
        int m_nFirstGarbageBlock = 0;
        /// <summary>
        /// 0x138	4	1	Offset of Resources Block
        /// </summary>
        int m_nFirstToolBlock = 0;
        /// <summary>
        /// 0x13C	4	1	Number of Symbol elements
        /// </summary>
        int m_numPointObjects = 0;
        /// <summary>
        /// 0x140	4	1	Number of Line elements
        /// </summary>
        int m_numLineObjects = 0;
        /// <summary>
        /// 0x144	4	1	Number of Region elements
        /// </summary>
        int m_numRegionObjects = 0;
        /// <summary>
        /// 0x148	4	1	Number of Text elements
        /// </summary>
        int m_numTextObjects = 0;
        /// <summary>
        /// 0x14C	4	1	MaxCoordBufSize
        /// </summary>
        int m_nMaxCoordBufSize = 0;

        /// 0x14E	14	1	14 Unknown bytes (Probably reserved and set to zero)

        //        For detailed information on distance unit values see:
        //        MapInfoProgramDirectory/Ut/Reproject/MapInfoUnits.db
        /// <summary>
        /// 0x15E Map File Distance Units
        /// </summary>
        byte m_nDistUnitsCode = 7;       // Meters

        /// <summary>
        /// 0x15F	1	1	Type of Element Indexing data (see also 0x130)
        /// 0 = NoData
        /// 1 = Object Definition Block (NoIndex block)
        /// 2 = Index Block
        /// </summary>
        byte m_nMaxSpIndexDepth = 0;
        /// <summary>
        /// 0x160	1	1	CoordPrecision
        /// Value:6 for Lat/Long maps
        /// Value:8 for Cartesian maps
        /// Value:1 for Projected maps
        /// </summary>
        byte m_nCoordPrecision = 3;      // ??? 3 Digits of precision
        /// <summary>
        /// 0x161	1	1	CoordOriginCode
        /// Value:2 for Lat/Long maps
        /// Value:1 for Cartesian and Projected maps
        /// </summary>
        byte m_nCoordOriginQuadrant = HDR_DEF_ORG_QUADRANT; // ???
        /// <summary>
        /// 0x162	1	1	ReflectAxisCode	
        /// Value:1 for Lat/Long maps
        /// Value:0 for Cartesian and Projected maps
        /// </summary>
        byte m_nReflectXAxisCoord = HDR_DEF_REFLECTXAXIS;
        /// <summary>
        /// 0x163	1	1	ObjLenArraySize	(at start of this block)
        /// </summary>
        byte m_nMaxObjLenArrayId = HDR_OBJ_LEN_ARRAY_SIZE - 1;  // See gabyObjLenArray[]
        /// <summary>
        /// 0x164	1	1	Number of pen resources
        /// </summary>
        byte m_numPenDefs = 0;
        /// <summary>
        /// 0x165	1	1	Number of brush resources
        /// </summary>
        byte m_numBrushDefs = 0;
        /// <summary>
        /// 0x166	1	1	Number of symbol resources
        /// </summary>
        byte m_numSymbolDefs = 0;
        /// <summary>
        /// 0x167	1	1	Number of text resources
        /// </summary>
        byte m_numFontDefs = 0;
        /// <summary>
        /// 0x168	2	1	Number of Resource Blocks
        /// </summary>
        short m_numMapToolBlocks = 0;

        TABProjInfo m_sProj = new TABProjInfo()
        {
            //0x16D	1	1	Projection type
            nProjId = 0,
            //0x16E	1	1	Datum (See also &H1C0, &H1C8, &H1D0)
            nEllipsoidId = 0,
            //&H16F	1	1	Units of coordinate system (Values equal to &H15E)
            nUnitsId = 7
        };

        double m_XScale = 1000.0;  // Default coord range (before SetCoordSysBounds()) 
        double m_YScale = 1000.0;  // will be [-1000000.000 .. 1000000.000]
        double m_XDispl = 0.0;
        double m_YDispl = 0.0;


        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="stream"></param>
        public TABMAPHeaderBlock(byte[] block)
            : base(block)
        {
            Position = 0x100;
            Read(ref MagicNumber);

            if (MagicNumber != HDR_MAGIC_COOKIE)
                throw new IOException(string.Format("Неверный Magic Cookie: есть {0} /n ожидалось {1}", MagicNumber, HDR_MAGIC_COOKIE));
            //  Переменные-члены инициализации
            //  Вместо того, чтобы в течение 30 Get / Set методы, мы сделаем все члены данных общественности и мы будем инициализировать их здесь.
            //  По этой причине, этот класс следует использовать с осторожностью.
            Read(ref m_nMAPVersionNumber);
            Read(ref m_nBlockSize);
            Read(ref m_dCoordsys2DistUnits);

            Read(ref m_nXMin);
            Read(ref m_nYMin);
            Read(ref m_nXMax);
            Read(ref m_nYMax);

            Position = 0x130;     // Skip 16 unknown bytes 

            Read(ref m_nFirstIndexBlock);
            Read(ref m_nFirstGarbageBlock);
            Read(ref m_nFirstToolBlock);

            Read(ref m_numPointObjects);
            Read(ref m_numLineObjects);
            Read(ref m_numRegionObjects);
            Read(ref m_numTextObjects);
            Read(ref m_nMaxCoordBufSize);

            Position = 0x15e;     // Skip 14 unknown bytes

            Read(ref m_nDistUnitsCode);
            Read(ref m_nMaxSpIndexDepth);
            Read(ref m_nCoordPrecision);
            Read(ref m_nCoordOriginQuadrant);
            Read(ref m_nReflectXAxisCoord);
            Read(ref m_nMaxObjLenArrayId);  // See gabyObjLenArray[]
            Read(ref m_numPenDefs);
            Read(ref m_numBrushDefs);
            Read(ref m_numSymbolDefs);
            Read(ref m_numFontDefs);
            Read(ref m_numMapToolBlocks);

            // DatumId никогда не был установлен (всегда 0), пока MapInfo 7.8. См ошибку 910 
            // MAP Номер версии составляет 500 в этом случае.
            Read(ref m_sProj.nDatumId);
            if (m_nMAPVersionNumber < HDR_VERSION_NUMBER) m_sProj.nDatumId = 0;

            ++Position;   // Skip unknown byte

            //&H16D	1	1	Projection type
            Read(ref m_sProj.nProjId);
            //&H16E	1	1	Datum (See also &H1C0, &H1C8, &H1D0)
            Read(ref m_sProj.nEllipsoidId);
            //&H16F	1	1	Units of coordinate system (Values equal to &H15E)
            Read(ref m_sProj.nUnitsId);

            Read(ref m_XScale);
            Read(ref m_YScale);
            Read(ref m_XDispl);
            Read(ref m_YDispl);

            //     In V.100 files, the scale and displacement do not appear to be set.
            //     we'll use m_nCoordPrecision to define the scale factor instead.
            //     
            if (m_nMAPVersionNumber <= 100)
            {
                m_XScale = m_YScale = Math.Pow(10.0, m_nCoordPrecision);
                m_XDispl = m_YDispl = 0.0;
            }

            for (byte i = 0; i < 6; i++)
            {
                Read(ref m_sProj.adProjParams[i]);
            }

            Read(ref m_sProj.dDatumShiftX);
            Read(ref m_sProj.dDatumShiftY);
            Read(ref m_sProj.dDatumShiftZ);


            //         In V.200 files, the next 5 datum params are unused and they
            //         * sometimes contain junk bytes... in this case we set adDatumParams[]
            //         * to 0 for the rest of the lib to be happy.
            for (byte i = 0; i < 5; i++)
            {
                Read(ref m_sProj.adDatumParams[i]);
                if (m_nMAPVersionNumber <= 200)
                    m_sProj.adDatumParams[i] = 0.0;
            }

        }

        public override void Add(byte[] block)
        {
            base.Add(block);
            Position = 0;
            //Array.Resize(ref read, TABRawBlock.Size * 2);
            //Array.Copy(block.read, 0, read, TABRawBlock.Size, TABRawBlock.Size);
            m_sProj.nAffineFlag = false;
            //if (m_nMAPVersionNumber >= 500 && m_nSizeUsed > 512)
            //{
            // Read Affine parameters A,B,C,D,E,F 
            // only if version 500+ and block is larger than 512 bytes
            byte nInUse = 0;
            Read(ref nInUse);
            if (nInUse != 0)
            {
                m_sProj.nAffineFlag = true;
                Read(ref m_sProj.nAffineUnits);
                Position += 6;
                //0x0208; // Skip unused bytes
                for (byte i = 0; i < 6; i++)
                {
                    Read(ref m_sProj.dAffineParam[i]);
                }
            }
            //}

        }

    }

    internal class TABMAPIndexBlock : TABRawBlock
    {
        public TABMAPIndexBlock(byte[] block)
        //: base(block)
        {
            Add(block);
        }

        public override void Add(byte[] block)
        {
            base.Add(block);
            Position = 1;
            if (link == null)
                link = new byte[1];
            else
                Array.Resize(ref link, link.Length + 1);
            Read(ref link[link.Length - 1]);
            Read(ref m_numEntries);
            //m_asEntries = new TABMAPIndexEntry[m_numEntries];
            for (byte i = 0; i < m_numEntries; i++)
            {
                TABMAPIndexEntry tmp = new TABMAPIndexEntry();
                Read(ref tmp.XMin);
                Read(ref tmp.YMin);
                Read(ref tmp.XMax);
                Read(ref tmp.YMax);
                Read(ref tmp.Id);
                m_asEntries.Add(tmp);
            }

        }

        #region
        //Index Block header (length: &H4)
        //---------------------------------------------------------------
        //&H0	1	1	Index Block identifier (Value: &H1) [!]
        //&H1	1	1	Link
        public byte[] link; //= new byte[0];
        //&H2	1	2	Number of Index data blocks
        public short m_numEntries = 0;

        //Index data (length: &H14)
        //---------------------------------------------------------------
        //&H0	4	4	Object Definition Block MBR (XMin, YMin, XMax, YMax)
        //&H10	4	1	Object Definition Block offset
        public List<TABMAPIndexEntry> m_asEntries = new List<TABMAPIndexEntry>();// = new TABMAPIndexEntry[TABMAPIndexEntry.TAB_MAX_ENTRIES_INDEX_BLOCK];

        // Use these to keep track of current block's MBR
        protected int m_nMinX = 1000000000;
        protected int m_nMinY = 1000000000;
        protected int m_nMaxX = -1000000000;
        protected int m_nMaxY = -1000000000;

        //protected TABBinBlockManager m_poBlockManagerRef;

        // Info about child currently loaded
        protected TABMAPIndexBlock m_poCurChild;
        protected int m_nCurChildIndex;
        // Also need to know about its parent
        protected TABMAPIndexBlock m_poParentRef;

        //int GetNumFreeEntries();
        public int GetNumEntries()
        {
            return m_numEntries;
        }
        //TABMAPIndexEntry GetEntry(int iIndex);

        //int AddEntry(TABMAPIndexEntry entry, bool bAddInThisNodeOnly);
        //int GetCurMaxDepth();
        //void GetMBR(ref int nXMin, ref int nYMin, ref int nXMax, ref int nYMax);
        //public int GetNodeBlockPtr()
        //{
        //    //return GetStartAddress();
        //}

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void SetMAPBlockManagerRef(ref TABBinBlockManager poBlockMgr);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void SetParentRef(TABMAPIndexBlock poParent);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void SetCurChildRef(TABMAPIndexBlock poChild, int nChildIndex);

        public int GetCurChildIndex()
        {
            return m_nCurChildIndex;
        }
        public TABMAPIndexBlock GetCurChild()
        {
            return m_poCurChild;
        }
        public TABMAPIndexBlock GetParentRef()
        {
            return m_poParentRef;
        }

        //    int SplitNode(GInt32 nNewEntryXMin, GInt32 nNewEntryYMin, GInt32 nNewEntryXMax, GInt32 nNewEntryYMax);
        //    int SplitRootNode(GInt32 nNewEntryXMin, GInt32 nNewEntryYMin, GInt32 nNewEntryXMax, GInt32 nNewEntryYMax);
        //    void UpdateCurChildMBR(GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax, GInt32 nBlockPtr);
        //    void RecomputeMBR();
        //    int InsertEntry(GInt32 XMin, GInt32 YMin, GInt32 XMax, GInt32 YMax, GInt32 nBlockPtr);
        //    int ChooseSubEntryForInsert(GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax);
        //    GInt32 ChooseLeafForInsert(GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax);
        //    int UpdateLeafEntry(GInt32 nBlockPtr, GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax);
        //    int GetCurLeafEntryMBR(GInt32 nBlockPtr, ref GInt32 nXMin, ref GInt32 nYMin, ref GInt32 nXMax, ref GInt32 nYMax);

        // Static utility functions for node splitting, also used by the TABMAPObjectBlock class.
        //    static double ComputeAreaDiff(GInt32 nNodeXMin, GInt32 nNodeYMin, GInt32 nNodeXMax, GInt32 nNodeYMax, GInt32 nEntryXMin, GInt32 nEntryYMin, GInt32 nEntryXMax, GInt32 nEntryYMax);
        //    static int PickSeedsForSplit(ref TABMAPIndexEntry pasEntries, int numEntries, int nSrcCurChildIndex, GInt32 nNewEntryXMin, GInt32 nNewEntryYMin, GInt32 nNewEntryXMax, GInt32 nNewEntryYMax, ref int nSeed1, ref int nSeed2);
        #endregion
    }

    /// <summary>
    /// Class to handle Read/Write operation on .MAP Object data Blocks (Type 02)
    /// </summary>
    internal class TABMAPObjectBlock : TABRawBlock
    {
        public TABMAPObjectBlock(byte[] block)
        //: base(block)
        {
            Add(block);
        }

        public override void Add(byte[] block)
        {
            base.Add(block);
            Position = 1;
            if (link == null)
                link = new byte[1];
            else
                Array.Resize(ref link, link.Length + 1);

            Read(ref link[link.Length - 1]);

            Read(ref m_numDataBytes);       /* Excluding 4 bytes header */

            Read(ref m_nCenterX);
            Read(ref m_nCenterY);

            Read(ref m_nFirstCoordBlock);
            Read(ref m_nLastCoordBlock);


            //m_nCurObjectOffset = -1;
            //m_nCurObjectId = -1;
            //m_nCurObjectType = -1;

            while (Position < m_numDataBytes + HeaderSize)
            {

                MapFileRecord poObj = new MapFileRecord();
                Read(ref poObj.ShapeType);
                Read(ref poObj.MBR.Id);
                switch ((GeometryType)poObj.ShapeType)
                {
                    case GeometryType.NONE:
                        //poObj = new TABMAPObjNone();
                        break;
                    #region GeometryType.SYMBOL_C
                    case GeometryType.SYMBOL_C:
                        //ShortPoint [ID 1] (length: &HA):        [?]
                        //&H0     1       1       Identifier (Value: &H1) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     2       2       Coordinate value
                        //&H9     1       1       Symbol type number from Resource Block
                        //TABMAPObjPoint ShortPoint = new TABMAPObjPoint(poObj);
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = m_nCenterX + ReadInt16(),
                            Y = m_nCenterY + ReadInt16()
                        });
                        Read(ref poObj.Symbol);
                        fetures.Add(poObj);
                        break; 
                    #endregion
                    #region GeometryType.SYMBOL
                    case GeometryType.SYMBOL:
                        //LongPoint [ID 2] (length: &HE):
                        //&H0     1       1       Identifier (Value: &H2) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       2       Coordinate value
                        //&HD     1       1       Symbol type number from Resource Block
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = ReadInt32(),
                            Y = ReadInt32()
                        });
                        Read(ref poObj.Symbol);
                        fetures.Add(poObj);
                        break; 
                    #endregion
                    //case GeometryType.FONTSYMBOL_C:
                    //case GeometryType.FONTSYMBOL:
                    //    poObj = new TABMAPObjFontPoint;
                    //break;
                    //  case GeometryType.CUSTOMSYMBOL_C:
                    //  case GeometryType.CUSTOMSYMBOL:
                    //    poObj = new TABMAPObjCustomPoint;
                    //    break;
                    #region GeometryType.LINE_C
                    case GeometryType.LINE_C:
                        //ShortLine [ID 4] (length: &HE):
                        //&H0     1       1       Identifier (Value: &H4) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       2       Coordinate value
                        //&HD     1       1       Line type number from Resource Block
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = m_nCenterX + ReadInt16(),
                            Y = m_nCenterY + ReadInt16()
                        });
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = m_nCenterX + ReadInt16(),
                            Y = m_nCenterY + ReadInt16()
                        });
                        Read(ref poObj.Symbol);
                        fetures.Add(poObj);
                        break; 
                    #endregion
                    #region GeometryType.LINE
                    case GeometryType.LINE:
                        //LongLine [ID 5] (length: &H16):
                        //&H0     1       1       Identifier (Value: &H5) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       4       MBR
                        //&H15	1	1       Line type number from Resource Block
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = ReadInt32(),
                            Y = ReadInt32()
                        });
                        poObj.Points.Add(new TABMAPVertex()
                        {
                            X = ReadInt32(),
                            Y = ReadInt32()
                        });
                        Read(ref poObj.Symbol);
                        fetures.Add(poObj);
                        break; 
                    #endregion
                    #region GeometryType.PLINE_C
                    case GeometryType.PLINE_C:
                        //ShortPolyline [ID 7] (length: &H1A):
                        //&H0     1       1       Identifier (Value: &H7) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       Offset of coordinate data in Coordinate Definition Block
                        //&H9     4       1       Bytes to read for coordinates from Coordinate Definition Block [?]
                        //&HD     2       2       Label location coordinates
                        //&H11    2       4       MBR     
                        //&H19    1       1       Line type number from Resource Block
                        Read(ref poObj.CoordBlockPtr);
                        Read(ref poObj.CoordDataSize);
                        poObj.LabelLocation = new TABMAPVertex()
                        {
                            X = m_nCenterX + ReadInt16(),
                            Y = m_nCenterY + ReadInt16()
                        };
                        poObj.MBR.XMin = m_nCenterX + ReadInt16();
                        poObj.MBR.YMin = m_nCenterY + ReadInt16();
                        poObj.MBR.XMax = m_nCenterX + ReadInt16();
                        poObj.MBR.YMax = m_nCenterY + ReadInt16();
                        Read(ref poObj.Symbol);
                        break;
                    #endregion  
                    #region GeometryType.PLINE
                    case GeometryType.PLINE:
                        //LongPolyline [ID 8] (length: &H26):
                        //&H0     1       1       Identifier (Value: &H8) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)
                        //&H5     4       1       Offset of coordinate data in Coordinate Definition Block
                        //&H9     4       1       Bytes to read for coordinates from Coordinate Definition Block [?]
                        //&HD     4       2       Label location coordinates
                        //&H15    4       4       MBR
                        //&H25    1       1       Line type number from Resource Block
                        Read(ref poObj.CoordBlockPtr);
                        Read(ref poObj.CoordDataSize);
                        poObj.LabelLocation = new TABMAPVertex()
                        {
                            X = ReadInt32(),
                            Y = ReadInt32()
                        };
                        poObj.MBR.XMin = ReadInt32();
                        poObj.MBR.YMin = ReadInt32();
                        poObj.MBR.XMax = ReadInt32();
                        poObj.MBR.YMax = ReadInt32();
                        Read(ref poObj.Symbol);

                        break; 
                    #endregion
                    #region GeometryType.REGION_C
                    case GeometryType.REGION_C:
                        //ShortRegion [ID 13] (length: &H25):
                        //&H0     1       1       Identifier (Value: &HD) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       Offset of coordinate data in Coordinate Definition Block
                        //&H9     4       1       Bytes to read for coordinates from Coordinate Definition Block [??]
                        //&HD     2       1       Section count
                        //&HF     4       2       Label X,Y
                        //&H13    4       4       MBR
                        //&H23    1       1       Line type number from Resource Block
                        //&H24    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.REGION
                    case GeometryType.REGION:
                        //LongRegion [ID 14] (length: &H29):
                        //&H0     1       1       Identifier (Value: &HE) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       Offset of coordinate data in Coordinate Definition Block
                        //&H9     4       1       Bytes to read for coordinates from Coordinate Definition Block [??]
                        //&HD     2       1       Section count
                        //&HF     4       2       Label X,Y
                        //&H17	4       4       MBR
                        //&H27    1       1       Line type number from Resource Block
                        //&H28    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    //  case GeometryType.MULTIPLINE_C:
                    //  case GeometryType.MULTIPLINE:
                    //  case GeometryType.V450_REGION_C:
                    //  case GeometryType.V450_REGION:
                    //  case GeometryType.V450_MULTIPLINE_C:
                    //  case GeometryType.V450_MULTIPLINE:
                    //  case GeometryType.V800_REGION_C:
                    //  case GeometryType.V800_REGION:
                    //  case GeometryType.V800_MULTIPLINE_C:
                    //  case GeometryType.V800_MULTIPLINE:
                    //    poObj = new TABMAPObjPLine;
                    //    break;
                    #region GeometryType.ARC_C
                    case GeometryType.ARC_C:
                        //ShortArc [ID 10] (length: &H16):
                        //&H0     1       1       Identifier (Value: &HA) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       2       MBR of defining ellipse
                        //&HD     4       2       MBR of the arc
                        //&H15    1       1       Line type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.ARC
                    case GeometryType.ARC:
                        //LongArc [ID 11] (length: &H26):
                        //&H0     1       1       Identifier (Value: &HB) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       4       MBR of defining ellipse
                        //&15     4       4       MBR of the arc
                        //&H25    1       1       Line type number from Resource Block
                        break; 
                    #endregion
                        //poObj = new TABMAPObjArc;
                    //    break;
                    #region GeometryType.RECT_C
                    case GeometryType.RECT_C:
                        //ShortRectangle [ID 19] (length: &HF):
                        //&H0     1       1       Identifier (Value: &H10) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     2       4       MBR
                        //&HD     1       1       Line type number in Resource Block
                        //&HE     1       1       Brush type number in Resource Block
                        break; 
                    #endregion
                    #region GeometryType.RECT
                    case GeometryType.RECT:
                        //LongRectangle [ID 20] (length: &H17):
                        //&H0     1       1       Identifier (Value: &H17) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       4       MBR
                        //&H15    1       1       Line type number from Resource Block
                        //&H16    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.ROUNDRECT_C
                    case GeometryType.ROUNDRECT_C:
                        //ShortRoundRectangle [ID 22] (length: &H13):
                        //&H0     1       1       Identifier (Value: &H16) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     2       1       XRadius
                        //&H7     2       1       YRadius
                        //&H9     2       4       MBR
                        //&H11    1       1       Line type number from Resource Block
                        //&H12    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.ROUNDRECT
                    case GeometryType.ROUNDRECT:
                        //LongRoundRectangle [ID 23] (length: &H1F):
                        //&H0     1       1       Identifier (Value: &H16) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       XRadius
                        //&H9     4       1       YRadius
                        //&HD     4       4       MBR
                        //&H1D    1       1       Line type number from Resource Block
                        //&H1E    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.ELLIPSE_C
                    case GeometryType.ELLIPSE_C:
                        //ShortEllipse [ID 25] (length: &HF):
                        //&H0     1       1       Identifier (Value: &H1A) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     2       4       MBR
                        //&HD	1       1       Line type number from Resource Block
                        //&HE	1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.ELLIPSE
                    case GeometryType.ELLIPSE:
                        //LongEllipse [ID 26] (length: &H17):
                        //&H0     1       1       Identifier (Value: &H1A) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       4       MBR
                        //&H15    1       1       Line type number from Resource Block
                        //&H16    1       1       Brush type number from Resource Block
                        break; 
                    #endregion
                    //    poObj = new TABMAPObjRectEllipse;
                    //    break;
                    #region GeometryType.TEXT_C
                    case GeometryType.TEXT_C:
                        //ShortText [ID 16] (length: &H27)
                        //&H0     1       1       Identifier (Value: &H10) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       Offset of text body in Coordinate Definition Block
                        //&H9     2       1       Number of characters in text body
                        //&HB     2       1       Justification spacing arrowtype:
                        //                                flag 2^1 - centered text 
                        //                                flag 2^2 - right aligned text 
                        //                                flag 2^3 - line spacing 1.5 
                        //                                flag 2^4 - line spacing 2.0 
                        //                                flag 2^5 - label line: simple 
                        //                                flag 2^6 - label line: arrow 
                        //&HD     2       1       Text rotation angle (0.1 degrees)
                        //&HF     1       1       FontStyle #1:
                        //                                flag 2^0 - bold text 
                        //                                flag 2^1 - italic text 
                        //                                flag 2^2 - underlined text 
                        //                                flag 2^3 - overlined text 
                        //                                flag 2^4 - unknown 
                        //                                flag 2^5 - shadowed text 
                        //&H10    1       1       FontStyle #2:
                        //                                flag 2^0 - box background 
                        //                                flag 2^1 - halo background 
                        //                                flag 2^2 - All Caps 
                        //                                flag 2^3 - Expanded
                        //&H11    3       1       Foreground color
                        //&H14    3       1       Background color
                        //&H17    2       2       Arrow endpoint coordinates
                        //&H1B    2       1	Height
                        //&H1D	1	1	Font name index
                        //&H1E    2       4       MBR
                        //&H26    1       1       Pen type from Resource Block
                        break; 
                    #endregion
                    #region GeometryType.TEXT
                    case GeometryType.TEXT:
                        //LongText [ID 17] (length: &H32)
                        //&H0     1       1       Identifier (Value: &H11) [!]
                        //&H1     4       1       RowID - Validity: (+0 = Valid; +&H40000000 = Deleted)       
                        //&H5     4       1       Offset of text body in Coordinate Definition Block
                        //&H9     2       1       Number of characters in text body
                        //&HC     2       1       Justification spacing arrowtype:
                        //                                flag 2^1 - centered text 
                        //                                flag 2^2 - right aligned text 
                        //                                flag 2^3 - line spacing 1.5 
                        //                                flag 2^4 - line spacing 2.0 
                        //                                flag 2^5 - label line: simple 
                        //                                flag 2^6 - label line: arrow 
                        //&HD     2       1       Text rotation angle (0.1 degrees)
                        //&HF     1       1       FontStyle #1:
                        //                                flag 2^0 - bold text 
                        //                                flag 2^1 - italic text 
                        //                                flag 2^2 - underlined text 
                        //                                flag 2^3 - overlined text 
                        //                                flag 2^4 - unknown 
                        //                                flag 2^5 - shadowed text 
                        //&H10    1       1       FontStyle #2:
                        //                                flag 2^0 - box background 
                        //                                flag 2^1 - halo background 
                        //                                flag 2^2 - All Caps 
                        //                                flag 2^3 - Expanded
                        //&H11    3       1       Foreground color
                        //&H14    3       1       Background color
                        //&H17    4       2       Arrow endpoint coordinates
                        //&H1F    1       4	Height
                        //&H20	1	1	Font name index
                        //&H30    4       4       MBR
                        //&H31    1       1       Pen type from Resource Block
                        break; 
                    #endregion
                    //    poObj = new TABMAPObjText;
                    //    break;
                    //  case GeometryType.MULTIPOINT_C:
                    //  case GeometryType.MULTIPOINT:
                    //  case GeometryType.V800_MULTIPOINT_C:
                    //  case GeometryType.V800_MULTIPOINT:
                    //    poObj = new TABMAPObjMultiPoint;
                    //    break;
                    //  case GeometryType.COLLECTION_C:
                    //  case GeometryType.COLLECTION:
                    //  case GeometryType.V800_COLLECTION_C:
                    //  case GeometryType.V800_COLLECTION:
                    //    poObj = new TABMAPObjCollection();
                    //break;
                    default:
                        throw new ArgumentException(poObj.ShapeType.ToString());
                    //break;
                }
            }
        }

        #region Var
        //Object Definition Block header (length: &H14)
        const byte HeaderSize = 20;
        //&H0     1       1	Object Definition Block identifier (Value: &H2) [!]
        //&H1     1       1	Link to next Object Definition Block
        public byte[] link; // = new byte[0];
        //&H2     2       1	Bytes To Follow (length of ODB data)
        public short m_numDataBytes; // Excluding first 4 bytes header 
        //&H4     4       4	Base coordinate values for short object types
        protected int m_nCenterX;
        protected int m_nCenterY;
        protected int m_nFirstCoordBlock;
        protected int m_nLastCoordBlock;

        public Collection<MapFileRecord> fetures = new Collection<MapFileRecord>();

        //Object Definition data items, which are identified by a code in the first byte, are
        //arrayed in an Object Definition Block after the header. The items in an Object
        //Definition Block reference coordinate and section definitions in  
        //an associated Coordinate Definition Block (or Blocks). For details about 
        //object types see Edwards' notes.

        //Объект элементы данных Определение, которые определены с помощью кода в первом байте, 
        //облеченные в определении объекта блока после заголовка. Элементы в объект ссылки 
        //определение блока координации и определения раздела в соответствующем координат 
        //определение блока (или блоков). Для получения подробной информации о типах объектов см заметки Эдвардса.

        // In order to compute block center, we need to keep track of MBR
        protected int m_nMinX = 1000000000;
        protected int m_nMinY = 1000000000;
        protected int m_nMaxX = -1000000000;
        protected int m_nMaxY = -1000000000;

        // Keep track of current object either in read or read/write mode
        protected int m_nCurObjectOffset; // -1 if there is no current object.
        protected int m_nCurObjectId; // -1 if there is no current object.
        protected int m_nCurObjectType; // -1 if there is no current object.

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    virtual int ReadIntCoord(GBool bCompressed, ref GInt32 nX, ref GInt32 nY);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int WriteIntCoord(GInt32 nX, GInt32 nY, GBool bCompressed);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int WriteIntMBRCoord(GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax, GBool bCompressed);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int UpdateMBR(GInt32 nX, GInt32 nY);

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int PrepareNewObject(ref TABMAPObjHdr poObjHdr);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int CommitNewObject(ref TABMAPObjHdr poObjHdr);

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void AddCoordBlockRef(GInt32 nCoordBlockAddress);
        public int GetFirstCoordBlockAddress()
        {
            return m_nFirstCoordBlock;
        }
        public int GetLastCoordBlockAddress()
        {
            return m_nLastCoordBlock;
        }

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void GetMBR(ref GInt32 nXMin, ref GInt32 nYMin, ref GInt32 nXMax, ref GInt32 nYMax);
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void SetMBR(GInt32 nXMin, GInt32 nYMin, GInt32 nXMax, GInt32 nYMax);

        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    void Rewind();
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //    int AdvanceToNextObject(TABMAPHeaderBlock NamelessParameter);
        public int GetCurObjectOffset()
        {
            return m_nCurObjectOffset;
        }
        public int GetCurObjectId()
        {
            return m_nCurObjectId;
        }
        public int GetCurObjectType()
        {
            return m_nCurObjectType;
        }
        #endregion


    }

    /// <summary>
    /// Entries found in type 1 blocks of .MAP files
    /// We will use this struct to rebuild the geographic index in memory
    /// Мы будем использовать эту структуру, чтобы восстановить географический индекс в памяти
    /// </summary>
    public struct TABMAPIndexEntry
    {
        public const short TAB_MAX_ENTRIES_INDEX_BLOCK = ((512 - 4) / 20);
        // These members refer to the info we find in the file
        // Эти члены относятся к информации, которую находим в файле
        public int XMin;
        public int YMin;
        public int XMax;
        public int YMax;
        public int Id;
    }

    public struct TABVertex
    {
        public double X;
        public double Y;
    }

    public struct TABMAPVertex
    {
        public int X;
        public int Y;
    }

    public class TABMAPObjHdr
    {
        public byte m_nType;
        public TABMAPIndexEntry MBR; // Object MBR 
        public TABMAPObjHdr() { }

        public TABMAPObjHdr(TABMAPObjHdr obj)
        {
            this.m_nType = obj.m_nType;
            this.MBR = obj.MBR;
        }

        //    static TABMAPObjHdr NewObj(GByte nNewObjType, GInt32 nId);
        //    static TABMAPObjHdr ReadNextObj(ref TABMAPObjectBlock poObjBlock, ref TABMAPHeaderBlock poHeader);

        /// <summary>
        /// Returns TRUE if the current object type uses compressed coordinates or FALSE otherwise.
        /// </summary>
        /// <returns></returns>
        private bool IsCompressedType()
        {
            // Compressed types are 1, 4, 7, etc.
            return ((m_nType % 3) == 1 ? true : false);
        }
        //    int WriteObjTypeAndId(TABMAPObjectBlock NamelessParameter);
        //    void SetMBR(GInt32 nMinX, GInt32 nMinY, GInt32 nMaxX, GInt32 mMaxY);

        //public virtual int WriteObj(ref TABMAPObjectBlock UnnamedParameter1)
        //{
        //    return -1;
        //}

        //  protected:
        //public virtual int ReadObj(ref TABMAPObjectBlock UnnamedParameter1)
        //{
        //    return -1;
        //}
    }

    /// <summary>
    /// Codes for the known MapInfo Geometry types
    /// </summary>
    public enum GeometryType : sbyte
    {
        NONE = 0,
        SYMBOL_C = 0x01,
        SYMBOL = 0x02,
        LINE_C = 0x04,
        LINE = 0x05,
        PLINE_C = 0x07,
        PLINE = 0x08,
        ARC_C = 0x0a,
        ARC = 0x0b,
        REGION_C = 0x0d,
        REGION = 0x0e,
        TEXT_C = 0x10,
        TEXT = 0x11,
        RECT_C = 0x13,
        RECT = 0x14,
        ROUNDRECT_C = 0x16,
        ROUNDRECT = 0x17,
        ELLIPSE_C = 0x19,
        ELLIPSE = 0x1a,
        MULTIPLINE_C = 0x25,
        MULTIPLINE = 0x26,
        FONTSYMBOL_C = 0x28,
        FONTSYMBOL = 0x29,
        CUSTOMSYMBOL_C = 0x2b,
        CUSTOMSYMBOL = 0x2c,
        //Version 450 object types:
        V450_REGION_C = 0x2e,
        V450_REGION = 0x2f,
        V450_MULTIPLINE_C = 0x31,
        V450_MULTIPLINE = 0x32,
        //Version 650 object types:
        MULTIPOINT_C = 0x34,
        MULTIPOINT = 0x35,
        COLLECTION_C = 0x37,
        COLLECTION = 0x38,
        //Version 800 object types:
        UNKNOWN1_C = 0x3a,
        UNKNOWN1 = 0x3b,
        V800_REGION_C = 0x3d,
        V800_REGION = 0x3e,
        V800_MULTIPLINE_C = 0x40,
        V800_MULTIPLINE = 0x41,
        V800_MULTIPOINT_C = 0x43,
        V800_MULTIPOINT = 0x44,
        V800_COLLECTION_C = 0x46,
        V800_COLLECTION = 0x47,
    }
    public class TABMAPObjHdrWithCoord : TABMAPObjHdr
    {
        public int m_nCoordBlockPtr;
        public int m_nCoordDataSize;

        //     Eventually this class may have methods to help maintaining refs to
        //     * coord. blocks when splitting object blocks.
        //     
    }

    public class TABMAPObjNone : TABMAPObjHdr
    {

        public TABMAPObjNone()
        {
        }

        //public virtual int WriteObj(ref TABMAPObjectBlock UnnamedParameter1)
        //{
        //    return 0;
        //}

        //  protected:
        //public virtual int ReadObj(ref TABMAPObjectBlock UnnamedParameter1)
        //{
        //    return 0;
        //}
    }


    public class TABMAPObjPoint : TABMAPObjHdr
    {
        public TABMAPVertex Position;
        public byte m_nSymbolId;

        public TABMAPObjPoint(TABMAPObjHdr obj) : base(obj) { }

        //    virtual int WriteObj(TABMAPObjectBlock NamelessParameter);

    }

}
