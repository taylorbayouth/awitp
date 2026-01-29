using UnityEngine;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UltimateProceduralPrimitives
{

    [CustomEditor(typeof(UPPImporter))]
    sealed class UPPImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty _lockParameters;
        SerializedProperty _meshName;
        SerializedProperty _UseFileNameAsMeshName;

        SerializedProperty _shape;

        SerializedProperty _plane;
        SerializedProperty _planeFlex;
        SerializedProperty _planeSuperEllipse;
        SerializedProperty _box;
        SerializedProperty _boxFlex;
        SerializedProperty _boxRounded;
        SerializedProperty _pyramid;
        SerializedProperty _pyramidFlex;
        SerializedProperty _pyramidPerfectTriangularFlex;
        SerializedProperty _boxSuperEllipsoid;
        SerializedProperty _boxSuperEllipsoidPlus;
        SerializedProperty _sphere;
        SerializedProperty _sphereIco;
        SerializedProperty _sphereFibonacci;
        SerializedProperty _tearDrop;
        SerializedProperty _cylinder;
        SerializedProperty _cone;
        SerializedProperty _clouds;
        SerializedProperty _supershape;

        SerializedProperty _meshCut;


        bool _isOpenAboutShape = false;
        bool _isOpenAboutVertices = false;
        bool _isOpenAboutMeshCut = false;



        public override void OnEnable()
        {
            base.OnEnable();

            _lockParameters = serializedObject.FindProperty("_lockParameters");
            _meshName = serializedObject.FindProperty("_meshName");
            _UseFileNameAsMeshName = serializedObject.FindProperty("_useFileNameAsMeshName");

            _shape = serializedObject.FindProperty("_shape");

            _plane = serializedObject.FindProperty("_plane");
            _planeFlex = serializedObject.FindProperty("_planeFlex");
            _planeSuperEllipse = serializedObject.FindProperty("_planeSuperEllipse");
            _box = serializedObject.FindProperty("_box");
            _boxFlex = serializedObject.FindProperty("_boxFlex");
            _boxRounded = serializedObject.FindProperty("_boxRounded");
            _boxSuperEllipsoid = serializedObject.FindProperty("_boxSuperEllipsoid");
            _boxSuperEllipsoidPlus = serializedObject.FindProperty("_boxSuperEllipsoidPlus");
            _pyramid = serializedObject.FindProperty("_pyramid");
            _pyramidFlex = serializedObject.FindProperty("_pyramidFlex");
            _pyramidPerfectTriangularFlex = serializedObject.FindProperty("_pyramidPerfectTriangularFlex");
            _sphere = serializedObject.FindProperty("_sphere");
            _sphereIco = serializedObject.FindProperty("_sphereIco");
            _sphereFibonacci = serializedObject.FindProperty("_sphereFibonacci");
            _tearDrop = serializedObject.FindProperty("_tearDrop");
            _cylinder = serializedObject.FindProperty("_cylinder");
            _cone = serializedObject.FindProperty("_cone");
            _clouds = serializedObject.FindProperty("_clouds");
            _supershape = serializedObject.FindProperty("_supershape");

            _meshCut = serializedObject.FindProperty("_meshCut");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // EditorGUILayout.PropertyField(_lockParameters);
            // Draw lock parameters checkbox first
            EditorGUILayout.PropertyField(_lockParameters, new GUIContent("🔒 Lock Parameters"));
            EditorGUILayout.PropertyField(_meshName, new GUIContent("Mesh Name"));
            EditorGUILayout.PropertyField(_UseFileNameAsMeshName, new GUIContent("Use File name as Mesh name"));

            bool isLocked = _lockParameters.boolValue;

            using (new EditorGUI.DisabledScope(isLocked))
            {
                EditorGUILayout.PropertyField(_shape);
                switch ((Shape)_shape.intValue)
                {
                    case Shape.Plane: EditorGUILayout.PropertyField(_plane); break;
                    case Shape.PlaneFlex: EditorGUILayout.PropertyField(_planeFlex); break;
                    case Shape.PlaneSuperEllipse: EditorGUILayout.PropertyField(_planeSuperEllipse); break;
                    case Shape.Box: EditorGUILayout.PropertyField(_box); break;
                    case Shape.BoxFlex: EditorGUILayout.PropertyField(_boxFlex); break;
                    case Shape.BoxRounded: EditorGUILayout.PropertyField(_boxRounded); break;
                    case Shape.BoxSuperEllipsoid: EditorGUILayout.PropertyField(_boxSuperEllipsoid); break;
                    case Shape.BoxSuperEllipsoidPlus: EditorGUILayout.PropertyField(_boxSuperEllipsoidPlus); break;
                    case Shape.Pyramid: EditorGUILayout.PropertyField(_pyramid); break;
                    case Shape.PyramidFlex: EditorGUILayout.PropertyField(_pyramidFlex); break;
                    case Shape.PyramidPerfectTriangularFlex: EditorGUILayout.PropertyField(_pyramidPerfectTriangularFlex); break;
                    case Shape.Sphere: EditorGUILayout.PropertyField(_sphere); break;
                    case Shape.SphereIco: EditorGUILayout.PropertyField(_sphereIco); break;
                    case Shape.SphereFibonacci: EditorGUILayout.PropertyField(_sphereFibonacci); break;
                    case Shape.TearDrop: EditorGUILayout.PropertyField(_tearDrop); break;
                    case Shape.Cylinder: EditorGUILayout.PropertyField(_cylinder); break;
                    case Shape.Cone: EditorGUILayout.PropertyField(_cone); break;
                    case Shape.Clouds: EditorGUILayout.PropertyField(_clouds); break;
                    case Shape.Supershape: EditorGUILayout.PropertyField(_supershape); break;

                }

                EditorGUILayout.PropertyField(_meshCut);
            }

            // HelpBox
            string helpMsg = "";
            switch ((Shape)_shape.intValue)
            {
                case Shape.Plane:
                    helpMsg = "\n"
                              + "This is a basic Plane. If you want to change the orientation, use Orientation or Direction. SurfaceType is meaningless because this is a basic Plane.\n"
                              + "-----------------------\n"
                              + "これは基本的なPlaneです。向きを変えたい場合は、OrientationかDirectionを使ってください。なお、基本的なPlaneのため、SurfaceTypeは意味がありません。\n"
                              + "";
                    break;
                case Shape.PlaneFlex:
                    helpMsg = "\n"
                              + "FlexPlane is a plane where each vertex can be moved. To change it, change the value of VertexOffsets, which can be set for each orientation. If you want to change the orientation, use Orientation or Direction. The operation is a little difficult, but you will get used to it.\n"
                              + "-----------------------\n"
                              + "FlexPlaneは、各頂点を移動することができるPlaneです。変更するには、VertexOffsetsの値を変更してください。VertexOffsetsは各オリエンテーションごとに設定することができます。操作は少し難しいですが、慣れてくると思います。\n"
                              + "";
                    break;
                case Shape.PlaneSuperEllipse:
                    helpMsg = "\n"
                              + "SuperEllipsePlane is an ellipse-shaped Plane. The roundness of each vertex can be adjusted by changing the values of N1, N2, N3, and N4. Values must be between 0.0 and 1.0 or greater than 1.0.\n"
                              + "-----------------------\n"
                              + "SuperEllipsePlaneは楕円型のPlaneです。 N1、N2、N3、N4の値を変更することで各頂点の丸みを調整することができます。値は0.0から1.0、または1.0以上を設定してください。\n"
                              + "";
                    break;
                case Shape.Box:
                    helpMsg = "\n"
                              + "This is a basic Box. Note that SurfaceType is meaningless for basic Box.\n"
                              + "-----------------------\n"
                              + "これは基本的なボックスです。なお、基本的なPlaneのため、SurfaceTypeは意味がありません。\n"
                              + "";
                    break;
                case Shape.BoxFlex:
                    helpMsg = "\n"
                              + "FlexBox is a box that allows each vertex to be moved. To change it, change the value of VertexOffsets. The operation is a little difficult, but you will get used to it.\n"
                              + "-----------------------\n"
                              + "FlexBoxは、各頂点を移動することができるBoxです。変更するには、VertexOffsetsの値を変更してください。操作は少し難しいですが、慣れてくると思います。\n"
                              + "";
                    break;
                case Shape.BoxRounded:
                    helpMsg = "\n"
                              + "RoundedBox is used to create a rounded box. If you change the value of Radius, you will see that the corners and edges are given a rounded appearance. For more beautiful roundness, use BoxSuperEllipsoid. Note: When you make the Box itself larger, make sure to increase the Radius value accordingly.\n"
                              + "-----------------------\n"
                              + "RoundedBoxは、丸みを帯びたボックスを作成するために使います。Radiusの値を変更してみると、角と辺に丸みが与えられることがわかると思います。より美しい丸みが必要な場合はBoxSuperEllipsoidをご利用ください。　備考：Box自体を大きくした場合は、それに応じてRadiusの値も大きくすることを忘れないようにしてください。\n"
                              + "";
                    break;
                case Shape.BoxSuperEllipsoid:
                    helpMsg = "\n"
                              + "SuperEllipsoid is upward compatible with RoundedBox. By adjusting the N1 and N2 parameters, the up-down rounding and side rounding can be set. The N value should be between 0.0 and 1.0, or greater than 1.0.\n"
                              + "-----------------------\n"
                              + "SuperEllipsoidはRoundedBoxの上位互換です。 N1とN2のパラメーターを調整することで、上下の丸みと側面の丸みを設定することができます。Nの値は0.0から1.0、または1.0以上を設定してください。\n"
                              + "";
                    break;
                case Shape.BoxSuperEllipsoidPlus:
                    helpMsg = "\n"
                              + "SuperEllipsoidPlus is an extended version of SuperEllipsoid. More items can be set. The N value should be between 0.0 and 1.0, or greater than 1.0.\n"
                              + "\n"
                              + "- N1_Top Edges: Sets the roundness of the top edges.\n"
                              + "- N1_Bottom Edges: Sets the roundness of the bottom edges.\n"
                              + "\n"
                              + "- N2_Right Front Edge: set the roundness of the right front edge.\n"
                              + "- N2_Right Back Edge: set the roundness of the right back edge.\n"
                              + "- N2_Left Front Edge: set the roundness of the left front edge.\n"
                              + "- N2_Left Back Edge: set the roundness of the left back edge.\n"
                              + "-----------------------\n"
                              + "SuperEllipsoidPlusはSuperEllipsoidの拡張版です。設定できる項目が増えました。Nの値は0.0から1.0、または1.0以上を設定してください。\n"
                              + "\n"
                              + "- N1_Top Edges: 上辺の丸みを設定します。\n"
                              + "- N1_Bottom Edges: 下辺の丸みを設定します。\n"
                              + "\n"
                              + "- N2_Right Front Edge: 右前方の辺の丸みを設定します。\n"
                              + "- N2_Right Back Edge: 右後方の辺の丸みを設定します。\n"
                              + "- N2_Left Front Edge: 左前方の辺の丸みを設定します。\n"
                              + "- N2_Left Back Edge: 左後方の辺の丸みを設定します。\n"
                              + "";
                    break;
                case Shape.Pyramid:
                    helpMsg = "\n"
                              + "This is a Pyramid. When used with Y_Axis, it is recommended to set PivotPosition to Down.\n"
                              + "\n"
                              + "これはPyramidです。 Y_Axisで利用する場合はPivotPositionをDownにすることをおすすめします。\n"
                              + "";
                    break;
                case Shape.PyramidFlex:
                    helpMsg = "\n"
                              + "FlexPyramid is a pyramid that allows each vertex to be moved. To change it, change the value of VertexOffsets. The operation is a little difficult, but you will get used to it.\n"
                              + "-----------------------\n"
                              + "FlexPyramidは、各頂点を移動することができるPyramidです。 変更するには、VertexOffsetsの値を変更してください。操作は少し難しいですが、慣れてくると思います。\n"
                              + "";
                    break;
                case Shape.PyramidPerfectTriangularFlex:
                    helpMsg = "\n"
                              + "FlexPerfectTriangularPyramid is a pyramid with perfect equilateral triangles on 3 sides. Set the length of one side in the Length parameter.  In addition, each vertex can be moved. To change it, change the value of VertexOffsets. The operation is a little difficult, but you will get used to it.\n"
                              + "-----------------------\n"
                              + "FlexPerfectTriangularPyramidは、完全な正三角形を3面に持つピラミッドです。 Lengthパラメーターに一辺の長さを設定して下さい。 加えて、各頂点を移動することができます。 変更するには、VertexOffsetsの値を変更してください。 操作は少し難しいですが、慣れてくると思います。\n"
                              + "";
                    break;
                case Shape.Sphere:
                    helpMsg = "\n"
                              + "This is a UVSphere.\n"
                              + "- If you want to create a vertical or horizontal ellipsoid mesh, use SuperEllipsoidBox and set the parameters N1 and N2 to 1, and the values of Height, Width or Depth to your liking.\n"
                              + "- If you want to create a symmetrical, equilateral triangular polyhedron, use Icosphere. It is a more uniform sphere than the UV sphere.\n"
                              + "- If you want to create a sphere with triangles(vertices, edges, and faces) spread evenly, please use FibonacciSphere. The Fibonacci sequence is used to draw vertices in a spiral. It is suitable for using shaders or further editing vertices because the vertices are easy to handle.\n"
                              + "-----------------------\n"
                              + "これはUV球体です。\n"
                              + "・縦長や横長の楕円形メッシュを作成したい場合は、SuperEllipsoidBoxを使い、パラメーターN1とN2を1にして、Height、WidthまたはDepthをお好きな値にしてください。\n"
                              + "・左右対称で正三角形の多面体を作りたい場合はIcosphereを使って下さい。UV球体よりも均一な球体になります。\n"
                              + "・三角形（頂点と辺と面）が均等に広がった球体を作りたい場合はFibonacciSphereをご利用ください。FibonacciSphereはフィボナッチ数列を用いて螺旋状に頂点を引いていきます。頂点が扱いやすいためシェーダーを使う場合や頂点をさらに編集する場合に向いています。\n"
                              + "";
                    break;
                case Shape.SphereIco:
                    helpMsg = "\n"
                              + "*** Caution ***\n"
                              + "It is dangerous to set Subdivision to 6 or higher. If you have the machine power, it may not be a problem.\n"
                              + "This is an ICO sphere.\n"
                              + "- It is a symmetrical, equilateral triangular polyhedron.\n"
                              + "- It is more uniform than the UV sphere\n"
                              + "- Some UVs are defective due to lack of vertices in the structure.\n"
                              + "-----------------------\n"
                              + "*** 注 意 ***\n"
                              + "Subdivisionを「6」以上にするのは危険です。マシンパワーがあるなら問題ないかもしれません。\n"
                              + "これはICO球です。\n"
                              + "・左右対称で正三角形の多面体です。\n"
                              + "・UV球体よりも均一な球体になります。\n"
                              + "・構造上頂点が足りないためUVの一部に不具合が生じます。\n"
                              + "";
                    break;
                case Shape.SphereFibonacci:
                    helpMsg = "\n"
                              + "This is an Fibonacci Sphere.\n"
                              + "- FibonacciSphere is a sphere with triangles (vertices, edges, and faces) spread evenly.\n"
                              + "- The Fibonacci sequence is used to draw vertices in a spiral.\n"
                              + "- It is suitable for using shaders or further editing vertices because the vertices are easy to handle. (Set to Smooth.)\n"
                              + "- Some UVs are defective due to lack of vertices in the structure.\n"
                              + "-----------------------\n"
                              + "これはフィボナッチ球体です。\n"
                              + "・FibonacciSphereは、三角形(頂点と辺と面)が均等に広がった球体です。\n"
                              + "・フィボナッチ数列を用いて螺旋状に頂点を引いています。\n"
                              + "・頂点が扱いやすいためシェーダーを使う場合や頂点をさらに編集する場合に向いています。（Smoothに設定して下さい。） \n"
                              + "・構造上頂点が足りないためUVの一部に不具合が生じます。\n"
                              + "";
                    break;
                case Shape.TearDrop:
                    helpMsg = "\n"
                              + "This is a Tear Drop\n"
                              + "-----------------------\n"
                              + "これは、涙です。\n"
                              + "";
                    break;
                case Shape.Cylinder:
                    helpMsg = "\n"
                              + "This is a Cylinder. TopRadius and BottomRadius can be used to change the size of the circle. You can also create pentagons and hexagons by setting SurfaceType to Flat and changing Columns. The parameter Caps determines whether TopRadius and BottomRadius are closed or not.\n"
                              + "-----------------------\n"
                              + "これはシリンダーです。 TopRadiusとBottomRadiusで円の大きさを変更することができます。 SurfaceTypeをFlatにしてColumnsを変更することで五角形や六角形を作成することもできます。 パラメーターのCapsはTopRadiusとBottomRadiusを閉じるかどうかです。\n"
                              + "";
                    break;
                case Shape.Cone:
                    helpMsg = "\n"
                              + "Cone is made from Cylinder. The parameters are the same as for Cylinder.\n"
                              + "-----------------------\n"
                              + "ConeはCylinderから作られています。パラメーターはCylinderと一緒です。\n"
                              + "";
                    break;
                case Shape.Clouds:
                    helpMsg = "\n"
                              + "*** Caution ***\n"
                              + "Machine power is required. Use caution when raising \"Resolution\". Don't forget to save in advance!\n"
                              + "This is clouds. You can create clouds by blending multiple spheres. Be careful to use a large number of spheres and a large value for \"Resolution\" as this will increase the processing load.\n"
                              + "\n"
                              + "The parameters are described as follows\n"
                              + "\n"
                              + "Target: Choose the setting you want to use. I suggest you first select \"Preset\" and \"Apply\" as is. If you are not satisfied with the granularity, try increasing the value of \"Resolution\" little by little.\n"
                              + "\n"
                              + "UseGlobalResolution: If you use, set to true\n"
                              + "\n"
                              + "GlobalResolution: This value overrides the \"Resolution\" that can be set for each Target.\n"
                              + "\n"
                              + "Resolution: This is an important parameter. It specifies the granularity of the mesh. The higher this value, the more beautiful the mesh will be, but the mesh size will increase and machine power will be required.\n"
                              + "Good Practice: "
                              + "- When the number of spheres is two, it is recommended to start with \"Resolution\" of \"32\" or less. When the number of spheres is small, a low Resolution value may be enough to produce a beautiful image.\n"
                              + "- Please consider increasing \"Resolution\" when you increase the number of spheres or when you are not satisfied with the smoothness.\n"
                              + "- Even if you want to generate a high-resolution mesh, it is better to edit with a resolution of ~64 at first, and then generate a mesh with a value of 128, 256 or higher when the shape is complete. Depending on the number of spheres and other factors, a value of 512 or higher can be very beautiful.\n"
                              + "- Low-Poly style: If you want to create a low-poly style mesh, set this value to 16-64 and set \"SurfaceType\" to \"Flat\".\n"
                              + "\n"
                              + "NumberOfSpheres: Enter the number of spheres to be used.\n"
                              + "\n"
                              + "Radius: Enter the radius of the sphere.\n"
                              + "\n"
                              + "Position: Enter the position of the sphere.\n"
                              + "\n"
                              + "BlendWithNextSphere: This parameter determines how much the sphere should blend with the sphere of the next element. Try \"1\", \"4\", \"16\", \"32\", and \"64\" to start with. The degree of blending will gradually increase. The degree of blending depends on the \"Resolution\" value and the size of the shape, so adjust based on these values. Note: The value set for the last element is not used.\n"
                              + "-----------------------\n"
                              + "*** 注意 ***\n"
                              + "マシンパワーが必要です。「Resolution」を上げる際は注意してください。事前にセーブを忘れずに!!\n"
                              + "これは雲です。複数の球体をブレンドすることで雲を作成することができます。球体の数と「Resolution」の値が大きいと処理負荷が大きくなりますので気をつけてご利用ください。\n"
                              + "\n"
                              + "パラメーターの説明は次の通りです。\n"
                              + "\n"
                              + "Target: 使う設定を選びます。まずは「Preset」を選択してそのまま「Apply」してみると良いと思います。粒度に満足できない場合は「Resolution」の値を少しずつ上げてみてください。\n"
                              + "\n"
                              + "UseGlobalResolution: 使用する場合は「true」を指定します。\n"
                              + "\n"
                              + "GlobalResolution: 各「Target」で設定できる「Resolution」をこの値で上書きします。\n"
                              + "\n"
                              + "Resolution: 重要な項目です。メッシュの粒度を指定します。この値が大きいほどメッシュは美しくなりますが、メッシュのサイズが増え、マシンパワーも必要になります。\n"
                              + "良い方法:"
                              + "・球体の数が2つの時は「Resolution」を「32」以下から始めることをおすすめします。球体の数が少ないと、低いResolution値でも充分な美しさを出せる場合があります。\n"
                              + "・球体を増やしたり、滑らかさに満足できない場合に「Resolution」をあげることをご検討ください。\n"
                              + "・マシンパワーによっては生成にとても時間がかかるため、高解像度のメッシュを生成したい場合でも一旦〜64程で編集して、形が完成したら128や256、またはそれ以上の値でメッシュを生成する方法が良いでしょう。球体の数などにもよりますが、512以上だととても美しくなります。\n"
                              + "・ローポリ: ローポリ風のメッシュを作りたい場合は、この値を16〜64程度にして「SurfaceType」を「Flat」にしてください。\n"
                              + "\n"
                              + "NumberOfSpheres: 使用する球体の数を指定します。\n"
                              + "\n"
                              + "Radius: 球体の半径を指定します。\n"
                              + "\n"
                              + "Position: 球体の位置を指定します。\n"
                              + "\n"
                              + "BlendWithNextSphere: この値は、その球体と次のエレメントの球体をどの程度ブレンドさせるかを設定するパラメーターです。まずは「1」「4」「16」「32」「64」で試してみてください。徐々にブレンドの程度が大きくなっていきます。「Resolution」の値やシェイプの大きさによってブレンドの程度が変わってくるのでこれらの値を基準に調整してください。備考：最後のエレメントに設定されているこの値は使用されません。\n"
                              + "";
                    break;
                case Shape.Supershape:
                    helpMsg = "\n"
                              + "Supershape is a mesh that consists of a very complex mathematical formula. The parameter Radius is the radius of the mesh and should be considered simply as the size of the mesh. The other parameters N1, N2, N3, M, A, and B affect the shape of the mesh.\n"
                              + "\n"
                              + "It is almost impossible to predict how each parameter will affect the shape. We recommend that you actually try to change the parameters.\n"
                              + "\n"
                              + "First, try leaving Segment at 100, setting both M to 0 and all other parameters to 1 and Apply. You should now have a perfect sphere. From there, try changing the value of M to 5 or the value of A. You will get the idea step by step.\n"
                              + "\n"
                              + "Again, it is impossible to predict how these parameters in Supershape will affect the shape. A Google search for 'Supershape' may help you find interesting shapes discovered by our predecessors.\n"
                              + "\n"
                              + "If you find interesting parameter values, please let us know via email or in the review section. If you like, we would be happy to register it as a Preset of UltimateProceduralPrimitives.\n"
                              + "-----------------------\n"
                              + "Supershapeはとても複雑な数式からなるMeshです。 パラメータのRadiusはMeshの半径を表していますので単純に大きさと考えてください。 他のパラメーターである N1、N2、N3、M、A、B が Meshの形状に影響を与えるパラメーターです。\n"
                              + "\n"
                              + "それぞれのパラメーターが形状へ与える影響を予測することは、ほぼ不可能です。 実際にパラメーターを変更してみることをおすすめします。\n"
                              + "\n"
                              + "まずは、Segmentを100のままにして、Mを両方とも0にして、他のパラメーターを全て1にしてApplyをしてみてください。 完全な球体ができたと思います。 そこからMの値を5にしてみたり、Aの値を変更したりしてください。段々とイメージがつくかと思います。\n"
                              + "\n"
                              + "繰り返しになりますが、Supershapeのこれらのパラメーターが形状へどのように影響を与えるかを予想することは不可能です。 Googleで「Supershape」で検索すると先人たちが発見した面白い形状を見つけることができるかもしれません。\n"
                              + "\n"
                              + "もし、面白いパラメーターの値を見つけることができたら、メールやレビュー欄でお知らせください。 もしよければ、UltimateProceduralPrimitivesの Preset ととして登録させていただければと思います。\n"
                              + "";
                    break;
                default:
                    helpMsg = "";
                    break;
            }

            string aboutVertexMsg = "\n"
                          + "Note on using vertices.\n"
                          + "There are the following differences depending on whether SurfaceType is set to \"Smooth\" or \"Flat\".\n"
                          + "There is also a difference when using \"MeshCut\". \n"
                          + "\n"
                          + "Smooth: The values that can be obtained with \"mesh.vertices\" are the vertex information calculated from the formula.There are essentially no duplicate vertices, and they are regularly aligned.\n"
                          + "\n"
                          + "Flat: The values that can be obtained with  \"mesh.vertices\" are the triangle information created based on the vertex information calculated from the formula.Since FlatSurface cannot be realized if vertices are shared, duplicate vertex information is added to create triangles. Ordering regularity is not guaranteed.\n"
                          + "\n"
                          + "From the above, for example, if you want to use vertices in the order of the Fibonacci formula, set Smooth.\n"
                          + "\n"
                          + "If you use mesh cut, duplicate vertex information will be added as when you select Flat. And the order of vertex information is not guaranteed.\n"
                          + "Note that triangle information can be obtained by \"mesh.triangles\".\n"
                          + "-----------------------\n"
                          + "頂点を利用する際の注意。\n"
                          + "SurfaceTypeを「Smooth」にするか「Flat」にするかで、以下の違いがあります。\n"
                          + "また、メッシュカットを利用した場合も違いがあります。\n"
                          + "\n"
                          + "Smooth:「mesh.vertices」で取得できる値は数式から算出された頂点情報です。頂点の重複は基本的になく、規則的に並んでいます。\n"
                          + "\n"
                          + "Flat:「mesh.vertices」で取得できる値は数式から算出された頂点情報を元に作成された三角形の情報です。頂点を共有するとFlatSurfaceを実現できないので、三角形を作るために重複した頂点情報を追加しています。順序の規則性は保証されていません。\n"
                          + "\n"
                          + "上記から、例えばフィボナッチ数式の順番で頂点を利用したい場合はSmoothを設定して下さい。\n"
                          + "\n"
                          + "メッシュカットを利用した場合は、Flatを選択した時のように重複した頂点情報が追加されます。そして頂点情報の順番は保証されません。\n"
                          + "なお、三角形の情報は「mesh.triangles」で取得できます。\n"
                          + "";

            string aboutMeshCutMsg = "\n"
                          + "MeshCut feature allows for further editing of the mesh. Multiple cuts can be made and a separate material can be set for each cross section. \n"
                          + "\n"
                          + "mesh cut usage is as follows \n"
                          + "1: Enter the number of cuts in \"Num Of Cuts\"\n"
                          + "2: Set the parameters.\n"
                          + "3: Press \"Apply\".\n"
                          + "\n"
                          + "The description of the parameters is as follows \n"
                          + "Do Mesh Cut: whether or not to perform the cut.\n"
                          + "Cut Point1~3: Specifies the position of the cut. Imagine an infinite plane passing through these three points. That plane will be the cross-section. The three points you specify must be triangles (not straight lines). The side that is clockwise is the cross section.\n"
                          + "Offset: Specifies the offset of the cross section (the infinite plane). \n"
                          + "Use Side: Specifies which side of the cut mesh to use. \n"
                          + "Caps: whether or not to close the cross section. \n"
                          + "Cap Material: turn on if you want to set a separate material for the cross section. If on, a sub-mesh will be generated. \n"
                          + "\n"
                          + "Hints and other notes.\n"
                          + "If you are not sure about the cut point, try running the program with the default settings and observe the results. Next, change the value of 0.5 to 0 and observe the results again. You should be able to get a rough idea.\n"
                          + "Turning on Cap Material will generate a submesh. If you need the vertex information of a cross section, use this to your advantage. For example, if you need the vertex information of the first cross section, you can use the following program to obtain it.\n"
                          + "using System.Linq;\n"
                          + "var subMesh1Vtx = GetComponent<MeshFilter>().mesh.GetIndices(1).Select(x => GetComponent<MeshFilter>().mesh.vertices[x]);\n"
                          + "If you want vertex information for the second cross-section, change GetIndices(1) to (2). \n"
                          + "Please also read About Vertices. \n"
                          + "UVs have been optimized but may not be what you want. Please edit the texture itself or adjust the Tiling and Offset of the material. \n"
                          + "-----------------------\n"
                          + "メッシュカット機能を利用することで、メッシュをさらに編集することができます。カットは複数でき、断面ごとに個別のマテリアルを設定できます。\n"
                          + "\n"
                          + "メッシュカットの使い方は次のとおりです。\n"
                          + "1: Num Of Cuts にカットの回数を入力する\n"
                          + "2: パラメーターを設定する\n"
                          + "3: Apply を押下する\n"
                          + "\n"
                          + "パラメータの説明は次のとおりです。\n"
                          + "Do Mesh Cut: カットを実施するか否かです。\n"
                          + "Cut Point1~3: カットの位置を指定します。この3点を通る無限に広がるプレーンをイメージしてください。そのプレーンが断面になります。指定する3点は三角形である必要があります（直線ではダメです）。時計回りになる方が断面になります。\n"
                          + "Offset: 断面（無限に広がるプレーン）のオフセットを指定します。\n"
                          + "Use Side: カットしたメッシュのどちらを使うかを指定します。\n"
                          + "Caps: 断面を閉じるか否かです。\n"
                          + "Cap Material: 断面に個別のマテリアルを設定する場合にオンにします。オンにするとサブメッシュが生成されます。\n"
                          + "\n"
                          + "ヒントとその他注意事項\n"
                          + "Cut Point がよくわからない場合はとりあえず初期設定で実行して結果を観察してみてください。次に0.5となっている部分を0に変えて実行し再度観察すると大体わかると思います。\n"
                          + "Cap Material をONにするとサブメッシュが生成されます。断面の頂点情報が必要な場合はこれを活用してください。例えば、1つ目の断面の頂点情報が欲しい場合は次のようなプログラムで取得可能です。\n"
                          + "using System.Linq;\n"
                          + "var subMesh1Vtx = GetComponent<MeshFilter>().mesh.GetIndices(1).Select(x => GetComponent<MeshFilter>().mesh.vertices[x]);\n"
                          + "2つ目の断面の頂点情報が欲しい場合は GetIndices(1) を (2) に変えてください。\n"
                          + "About Vertices もご一読ください。\n"
                          + "UVは最適化してありますが希望に添えていないかもしれません。テクスチャ自体を編集したり、マテリアルのTilingやOffsetを調整しご対応ください。\n"
                          + "";



            // need to write this code after switch section.
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();

            bool isOpenAboutShape = EditorGUILayout.Foldout(_isOpenAboutShape, "About This Shape");
            bool isOpenAboutVertices = EditorGUILayout.Foldout(_isOpenAboutVertices, "About Vertices");
            bool isOpenAboutMeshCut = EditorGUILayout.Foldout(_isOpenAboutMeshCut, "About MeshCut");

            if (_isOpenAboutShape != isOpenAboutShape)
                _isOpenAboutShape = isOpenAboutShape;
            if (_isOpenAboutVertices != isOpenAboutVertices)
                _isOpenAboutVertices = isOpenAboutVertices;
            if (_isOpenAboutMeshCut != isOpenAboutMeshCut)
                _isOpenAboutMeshCut = isOpenAboutMeshCut;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            //開いている時はGUI追加
            if (isOpenAboutShape)
            { EditorGUILayout.HelpBox(helpMsg, MessageType.Info); }
            if (isOpenAboutVertices)
            { EditorGUILayout.HelpBox(aboutVertexMsg, MessageType.Info); }
            if (isOpenAboutMeshCut)
            { EditorGUILayout.HelpBox(aboutMeshCutMsg, MessageType.Info); }

            EditorGUILayout.EndVertical();

        }


        [MenuItem("Assets/Create/UltimateProceduralPrimitives")]
        public static void CreateNewAsset()
          => ProjectWindowUtil.CreateAssetWithContent("New UltimateProceduralPrimitive.upp", "");
    }
}
