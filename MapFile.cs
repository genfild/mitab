///http://spatialnews.geocomm.com/features/mif_format/
///

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Linq;

namespace MapInfo.IO
{
    /// <summary>
    /// Откройте файл .map, и инициализировать структуры, чтобы быть готовыми, чтобы прочитать объекты из него.
    /// Поскольку .map и .id файлы не являются обязательными, 
    /// вы можете установить bNoErrorMsg = TRUE, чтобы отключить сообщение об ошибке и получите обратный значение 1, 
    /// если файл не может быть открыт.
    /// В этом случае, только методы MoveToObjId () и GetCurObjType () могут быть использованы. 
    /// Они будут вести себя так, как будто файл .id содержал только пустые ссылки, 
    /// так что все объект будет выглядеть, как нет у них геометрии.
    /// </summary>
    class MapFile
    {
        //public int m_nMinTABVersion = 300;
        //private Collection<mapFileRecord> _records = new Collection<mapFileRecord>();

        public TABMAPHeaderBlock header;
        public TABMAPIndexBlock index;
        public TABMAPObjectBlock objects;

        public void Open(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            // читаем геометрию из файла .map
            string mapFile = fileName.ToLower().Replace(".tab", ".map");
            //int[] offsets;

            // .map-файл необходим по спецификации, но прочесть shape-файл можно и без него.
            if (File.Exists(mapFile))
                //offsets = ReadIndex(mapFile);
            //else
            //    offsets = new int[] { };

                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        while (stream.Position < stream.Length)
                        {
                            try
                            {
                                if (stream.Position == 0)
                                {
                                    header = new TABMAPHeaderBlock(TABRawBlock.GetBlock(stream));
                                }
                                else if (stream.Position == TABRawBlock.Size &&
                                    header.m_nMAPVersionNumber == TABMAPHeaderBlock.HDR_VERSION_NUMBER)
                                {
                                    header.Add(TABRawBlock.GetBlock(stream));
                                }
                                else
                                {
                                    byte[] blk = TABRawBlock.GetBlock(stream);
                                    switch (TABRawBlock.GetBlockClass(blk))
                                    {
                                        case SupportedBlockTypes.TABMAP_INDEX_BLOCK:
                                            if (index == null)
                                                index = new TABMAPIndexBlock(blk);
                                            else
                                                index.Add(blk);
                                            break;
                                        case SupportedBlockTypes.TABMAP_OBJECT_BLOCK:
                                            if (objects == null)
                                                objects = new TABMAPObjectBlock(blk);
                                            else
                                                objects.Add(blk);
                                            break;
                                        default:
                                            break;
                                    }
                                }

                            }
                            catch (IOException)
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        stream.Flush();
                        stream.Close();
                    }
                }

            //string dbaseFile = fileName.ToLower().Replace(".shp", ".dbf");
            ////dbaseFile = dbaseFile.Replace(".SHP", ".DBF");

            ////!!!
            //this.ReadAttributes(dbaseFile);
        }

        /// <summary>
        /// Returns the appropriate class to convert a shaperecord to an MapAround geometry given the type of shape.
        /// </summary>
        /// <param name="type">The shape file type.</param>
        /// <returns>An instance of the appropriate handler to convert the shape record to a Geometry</returns>
        //internal static ShapeHandler GetShapeHandler(ShapeType type)
        //{
        //    switch (type)
        //    {
        //        case ShapeType.Point:
        //            //case ShapeGeometryType.PointM:
        //            //case ShapeGeometryType.PointZ:
        //            //case ShapeGeometryType.PointZM:
        //            return new MapPointShapeHandler();

        //        //case ShapeType.Polygon:
        //        //    //case ShapeGeometryType.PolygonM:
        //        //    //case ShapeGeometryType.PolygonZ:
        //        //    //case ShapeGeometryType.PolygonZM:
        //        //    return new PolygonHandler();

        //        //case ShapeType.Polyline: //.LineString:
        //        //    //case ShapeGeometryType.LineStringM:
        //        //    //case ShapeGeometryType.LineStringZ:
        //        //    //case ShapeGeometryType.LineStringZM:
        //        //    return new MultiLineHandler();

        //        //case ShapeType.Multipoint:
        //        //    //case ShapeGeometryType.MultiPointM:
        //        //    //case ShapeGeometryType.MultiPointZ:
        //        //    //case ShapeGeometryType.MultiPointZM:
        //        //    return new MultiPointHandler();

        //        default:
        //            string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, "ShapeType {0} is not supported.", (int)type);
        //            throw new InvalidDataException(msg);
        //    }
        //}


    }

    /// <summary>
    /// Represents a record of shape file.
    /// </summary>
    public class MapFileRecord
    {
        #region Private fields

        private byte _shapeType;

        public TABMAPIndexEntry MBR;

        private int _contentLength;

        private Collection<int> _parts = new Collection<int>();
        private Collection<ICoordinate> _points = new Collection<ICoordinate>();

        private DataRow _attributes;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MapAround.IO.ShapeFileRecord 
        /// </summary>
        public MapFileRecord()
        {
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets or sets the length (in bytes) of this record.
        /// </summary>
        public int ContentLength
        {
            get { return _contentLength; }
            set { _contentLength = value; }
        }

        /// <summary>
        /// Gets or sets the shape type.
        /// </summary>
        public byte ShapeType
        {
            get { return _shapeType; }
            set { _shapeType = value; }
        }

        /// <summary>
        /// Gets a number of parts of the geometry.
        /// </summary>
        public int NumberOfParts
        {
            get { return _parts.Count; }
        }

        /// <summary>
        /// Gets a number of points of the geometry.
        /// </summary>
        public int NumberOfPoints
        {
            get { return _points.Count; }
        }

        /// <summary>    
        /// Gets a collection containing the indices of 
        /// coordinate sequences corresponding parts of
        /// geometry.
        /// </summary>
        public Collection<int> Parts
        {
            get { return _parts; }
        }

        /// <summary>
        /// Gets a collection of coordinates of
        /// the geometry.
        /// </summary>
        public Collection<ICoordinate> Points
        {
            get { return _points; }
        }

        /// <summary>
        /// Gets or sets an attributes row associated
        /// with this  record.
        /// </summary>
        public DataRow Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Returns a System.String that represents the current MapAround.IO.ShapeFileRecord.
        /// </summary>
        /// <returns>A System.String that represents the current MapAround.IO.ShapeFilerecord</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileRecord: RecordNumber={0}, ContentLength={1}, ShapeType={2}",
                this._recordNumber, this._contentLength, this._shapeType);

            return sb.ToString();
        }
        #endregion Public methods
    }

}
