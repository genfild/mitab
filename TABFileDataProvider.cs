using System;
using System.Collections.Generic;

using MapAround.IO;
using MapAround.Geometry;
using MapAround.Mapping;
using MapAround.Indexing;
using MapAround.Caching;

namespace MapAround.DataProviders
{

    /// <summary>
    /// Экземпляр класса обеспечивает доступ к данным, хранящимся в MapInfo формате TAB-файла.
    /// </summary>
    public class TABFileSpatialDataProvider : SpatialDataProviderBase
    {
        private string _fileName = string.Empty;
        private bool _processAttributes = false;
        private System.Text.Encoding _attributesEncoding = System.Text.Encoding.ASCII;
        private IFeatureCollectionCacheAccessor _cacheAccessor = null;

        /// <summary>
        /// Кэш объектов средства доступа.
        /// <para>
        /// Если объект кэша средства доступа был назначен, первый запрос данных считывает весь файл 
        /// и помещает данные в кэш. Последующие запросы привести к попыткам найти нужные данные в кэш-памяти, 
        /// и только если есть никто не читает файл снова.
        /// </para>
        /// <remarks>
        /// Слои использовать значение свойства псевдоним в качестве ключа доступа. 
        /// Убедитесь, что значение этого свойства не является нулевым, а не пустая строка.
        /// </remarks>
        /// </summary>
        public IFeatureCollectionCacheAccessor CacheAccessor
        {
            get { return _cacheAccessor; }
            set { _cacheAccessor = value; }
        }

        #region Свойства

        /// <summary>
        /// Получает или задает кодировку атрибутов.
        /// </summary>
        public System.Text.Encoding AttributesEncoding
        {
            get { return _attributesEncoding; }
            set { _attributesEncoding = value; }
        }

        /// <summary>
        /// Получает или задает значение, указывающее, будет ли обработаны атрибуты.
        /// </summary>
        public bool ProcessAttributes
        {
            get { return _processAttributes; }
            set { _processAttributes = value; }
        }

        /// <summary>
        /// Получает или задает имя файла.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        #endregion

        /// <summary>
        /// Вызывает, когда новая функция вызвана.
        /// </summary>
        public event EventHandler<FeatureOperationEventArgs> FeatureFetched;

        /// <summary>
        /// Создает и сохраняет индекс
        /// </summary>
        /// <param name="featureType"></param>
        /// <param name="b"></param>
        /// <param name="settings"></param>
        /// <param name="features"></param>
        private void buildAndSaveIndex(MapAround.Mapping.FeatureType featureType, 
                               BoundingRectangle b,
                               IndexSettings settings, 
                               IEnumerable<Feature> features)
        {
            throw new NotImplementedException();
        //    ISpatialIndex index = null;

        //    if (b.IsEmpty())
        //        b = new BoundingRectangle(0, 0, 0, 0);

        //    if (settings.IndexType == "QuadTree")
        //        index = new QuadTree(b);
        //    if (index == null)
        //        index = new KDTree(b);

        //    index.MaxDepth = settings.MaxDepth;
        //    index.BoxSquareThreshold = settings.BoxSquareThreshold;
        //    index.MinObjectCount = settings.MinFeatureCount;

        //    index.Build(features);

        //    _cacheAccessor.SaveFeaturesIndex(index, featureType);
        }

        private int internalQueryFeatures(IFeatureReceiver fr, BoundingRectangle bounds, bool checkBounds)
        {
            if (_cacheAccessor != null && !string.IsNullOrEmpty(fr.Alias))
            {
                _cacheAccessor.Key = fr.Alias;
                if (_cacheAccessor.ExistsInCache)
                    return FillFromCache(_cacheAccessor, fr, bounds, _processAttributes);
                else
                    // Если объект не найден в кэше, вы должны удалить все объекты из файла, чтобы поместить их в кэш
                    checkBounds = false;
            }

            MapFile shapeFile = new MapFile();
            //shapeFile.AttributesEncoding = _attributesEncoding;
            shapeFile.Open(_fileName); //, checkBounds ? bounds : null
            
        //    if (ProcessAttributes)
        //    {
        //        fr.FeatureAttributeNames.Clear();
        //        foreach (string s in shapeFile.AttributeNames)
        //            fr.FeatureAttributeNames.Add(s);
        //    }

            int result = 0;
            string layerHashStr = fr.GetHashCode().ToString();

        //    List<Feature> points = new List<Feature>();
        //    List<Feature> multiPoints = new List<Feature>();
        //    List<Feature> polylines = new List<Feature>();
        //    List<Feature> polygons = new List<Feature>();

            foreach (TABMAPObjHdr record in shapeFile.objects.fetures)
            {
        //        if (!checkBounds ||
        //            (record.MaxX >= bounds.MinX && record.MaxY >= bounds.MinY &&
        //             record.MinX <= bounds.MaxX && record.MinY <= bounds.MaxY))
        //        {
                Feature newFeature = null;
                IGeometry geometry = geometryFromShapeRecord(record);
        //            if (geometry != null)
        //            {
        //                newFeature = new Feature(geometry);
        //                newFeature.UniqKey = layerHashStr + record.RecordNumber.ToString();
        //                if (ProcessAttributes && record.Attributes != null)
        //                    newFeature.Attributes = record.Attributes.ItemArray;

        //                if (processFeature(newFeature, fr, points, multiPoints, polylines, polygons))
        //                    result++;
        //            }
        //        }
            }

        //    // If the objects are not extracted from the cache may be added to the cache.
        //    // This should be done only if the retrieval of all objects (checkBounds == false)
        //    if (_cacheAccessor != null && !string.IsNullOrEmpty(fr.Alias) &&
        //        checkBounds == false)
        //    {
        //        addFeaturesToCache(fr, points, multiPoints, polylines, polygons);
        //    }

            return result;
        }

        //private IGeometry geometryFromMapRecord(mapFileRecord record)
        //{
        //    switch (record.ShapeType)
        //    {
        //        // point
        //        case 1:
        //            return new PointD(record.Points[0].X, record.Points[0].Y);
        //        // polyline
        //        case 3:
        //            Polyline polyline = new Polyline();
        //            for (int i = 0; i < record.Parts.Count; i++)
        //            {
        //                LinePath path = new LinePath();
        //                int j;
        //                for (j = record.Parts[i]; j < (i == record.Parts.Count - 1 ? record.Points.Count : record.Parts[i + 1]); j++)
        //                    path.Vertices.Add(PlanimetryEnvironment.NewCoordinate(record.Points[j].X, record.Points[j].Y));

        //                polyline.Paths.Add(path);
        //            }
        //            return polyline;
        //        // ground
        //        case 5:
        //            Polygon p = new Polygon();
        //            for (int i = 0; i < record.Parts.Count; i++)
        //            {
        //                Contour contour = new Contour();
        //                int j;
        //                for (j = record.Parts[i]; j < (i == record.Parts.Count - 1 ? record.Points.Count : record.Parts[i + 1]); j++)
        //                    contour.Vertices.Add(PlanimetryEnvironment.NewCoordinate(record.Points[j].X, record.Points[j].Y));

        //                contour.Vertices.RemoveAt(contour.Vertices.Count - 1);
        //                p.Contours.Add(contour);
        //            }
        //            if (p.CoordinateCount > 0)
        //                return p;
        //            else
        //                return null;
        //        // set of points
        //        case 8:
        //            MultiPoint mp = new MultiPoint();
        //            for (int i = 0; i < record.Points.Count; i++)
        //                mp.Points.Add(PlanimetryEnvironment.NewCoordinate(record.Points[i].X, record.Points[i].Y));
        //            return mp;
        //    }

        //    return null;
        //}

        private bool processFeature(Feature feature, IFeatureReceiver fr, List<Feature> points,
            List<Feature> multiPoints,
            List<Feature> polylines,
            List<Feature> polygons)
        {
            if (feature == null)
                return false;

            bool isAccepted = true;
            if (FeatureFetched != null)
            {
                FeatureOperationEventArgs foea = new FeatureOperationEventArgs(feature);
                FeatureFetched(this, foea);
                isAccepted = foea.IsAccepted;
            }

            if (!isAccepted)
                return false;

            fr.AddFeature(feature);
            switch (feature.FeatureType)
            {
                case FeatureType.Point: points.Add(feature); break;
                case FeatureType.MultiPoint: multiPoints.Add(feature); break;
                case FeatureType.Polyline: polylines.Add(feature); break;
                case FeatureType.Polygon: polygons.Add(feature); break;
            }

            return true;
        }

        private void addFeaturesToCache(IFeatureReceiver fr, 
            List<Feature> points, 
            List<Feature> multiPoints,
            List<Feature> polylines,
            List<Feature> polygons)
        {
            _cacheAccessor.Key = fr.Alias;
            if (!_cacheAccessor.ExistsInCache)
            {
                BoundingRectangle b = new BoundingRectangle();
                List<Feature> pts = new List<Feature>();
                foreach (Feature feature in points)
                {
                    b.Join(feature.BoundingRectangle);
                    pts.Add(feature);
                }
                foreach (Feature feature in multiPoints)
                {
                    b.Join(feature.BoundingRectangle);
                    pts.Add(feature);
                }

                //buildAndSaveIndex(MapAround.Mapping.FeatureType.Point, b, fr.DefaultPointsIndexSettings, pts);

                b = new BoundingRectangle();
                foreach (Feature feature in polylines)
                    b.Join(feature.BoundingRectangle);

                //buildAndSaveIndex(MapAround.Mapping.FeatureType.Polyline, b, fr.DefaultPolylinesIndexSettings, polylines);

                b = new BoundingRectangle();
                foreach (Feature feature in polygons)
                    b.Join(feature.BoundingRectangle);

                //buildAndSaveIndex(MapAround.Mapping.FeatureType.Polygon, b, fr.DefaultPolygonsIndexSettings, polygons);

                if (_processAttributes)
                    _cacheAccessor.SaveAttributeNames(fr.FeatureAttributeNames);
            }
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver, BoundingRectangle bounds)
        {
            throw new NotImplementedException("QueryFeatures");
            //return internalQueryFeatures(receiver, bounds, true);
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver)
        {
            //throw new NotImplementedException("QueryFeatures(IFeatureReceiver receiver)");
            return internalQueryFeatures(receiver, new BoundingRectangle(), false);
        }
    }

    /// <summary>
    /// TAB-файл владелец поставщика данных.
    /// </summary>
    public class TABFileSpatialDataProviderHolder : SpatialDataProviderHolderBase
    {
        private static string[] _parameterNames = { "file_name", "process_attributes", "attributes_encoding" };
        private Dictionary<string, string> _parameters = null;


        /// <summary>
        /// Устанавливает значения параметров.
        /// </summary>
        /// <param name="parameters">Значения параметров</param>
        public override void SetParameters(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("file_name"))
                throw new ArgumentException("Missing parameter \"file_name\".");
            _parameters = parameters;
        }

        /// <summary>
        /// Получает список, содержащий имена параметров.
        /// </summary>
        /// <returns>Список, содержащий имена параметров</returns>
        public override string[] GetParameterNames()
        {
            return _parameterNames;
        }

        private ISpatialDataProvider createProviderInstance()
        {
            ShapeFileSpatialDataProvider provider = new ShapeFileSpatialDataProvider();
            if (_parameters == null)
                throw new InvalidOperationException("Parameter values not set.");

            provider.FileName = _parameters["file_name"];
            if (_parameters.ContainsKey("process_attributes"))
                provider.ProcessAttributes = _parameters["process_attributes"] == "1";
            if (_parameters.ContainsKey("attributes_encoding"))
                provider.AttributesEncoding = System.Text.Encoding.GetEncoding(_parameters["attributes_encoding"]);

            return provider;
        }

        /// <summary>
        /// Выполняет процедуру финализации для пространственного поставщика данных.
        /// Эта реализация ничего не делает.
        /// </summary>
        /// <param name="provider">Экземпляр провайдера Пространственных данных</param>
        public override void ReleaseProviderIfNeeded(ISpatialDataProvider provider)
        {

        }

        /// <summary>
        /// Инициализирует новый экземпляр
        /// </summary>
        public TABFileSpatialDataProviderHolder() : base("MapAround.DataProviders.TABFileSpatialDataProviderHolder")
        {
            GetProviderMethod = createProviderInstance;
        }
    }


}
