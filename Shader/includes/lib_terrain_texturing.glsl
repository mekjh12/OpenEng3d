
// 높이값에 따라 지형 텍스처를 블렌딩하는 함수
vec4 BlendTerrainTextures(float Height, vec2 Tex3, float texScale,
    sampler2D tex0, sampler2D tex1, sampler2D tex2, 
    sampler2D tex3, sampler2D tex4, sampler2D detailMap,
    bool useDetail, float h0, float h1, float h2, float h3, float h4)
{
   vec4 TexColor;
   vec2 ScaledTexCoord = Tex3 * texScale;

   if (Height < h0) {
      TexColor = texture(tex0, ScaledTexCoord);
   } 
   else if (Height < h1) {
      vec4 Color0 = texture(tex0, ScaledTexCoord);
      vec4 Color1 = texture(tex1, ScaledTexCoord);
      float Delta = h1 - h0;
      float Factor = (Height - h0) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < h2) {
      vec4 Color0 = texture(tex1, ScaledTexCoord);
      vec4 Color1 = texture(tex2, ScaledTexCoord);
      float Delta = h2 - h1;
      float Factor = (Height - h1) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < h3) {
      vec4 Color0 = texture(tex2, ScaledTexCoord);
      vec4 Color1 = texture(tex3, ScaledTexCoord);
      float Delta = h3 - h2;
      float Factor = (Height - h2) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else if (Height < h4) {
      vec4 Color0 = texture(tex3, ScaledTexCoord);
      vec4 Color1 = texture(tex4, ScaledTexCoord);
      float Delta = h4 - h3;
      float Factor = (Height - h3) / Delta;
      TexColor = mix(Color0, Color1, Factor);
   } 
   else {
      TexColor = texture(tex4, ScaledTexCoord);
   }

   if (useDetail) {
       TexColor *= texture(detailMap, ScaledTexCoord);
   }

   return TexColor;
}