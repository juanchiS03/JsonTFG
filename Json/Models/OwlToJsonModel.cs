using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Json.Models
{
    public class OwlToJsonModel
    {
        // Lista de clases en el modelo OWL
        public List<OwlClass> Classes { get; set; } = new List<OwlClass>();

        // Lista de propiedades en el modelo OWL
        public List<OwlProperty> Properties { get; set; } = new List<OwlProperty>();

        // Lista de relaciones entre los elementos en el modelo OWL
        public List<OwlRelationship> Relationships { get; set; } = new List<OwlRelationship>();
    }

    public class OwlClass
    {
        /// <summary>
        /// Nombre de la clase
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Etiqueta de la clase, puede ser opcional
        /// </summary>
        public string Label { get; set; } = null;
    }

    public class OwlProperty
    {
        /// <summary>
        /// Nombre de la propiedad
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Etiqueta de la propiedad, puede ser opcional
        /// </summary>
        public string Label { get; set; } = null;

        /// <summary>
        /// Comentario de la propiedad, puede ser opcional
        /// </summary>
        public string Comment { get; set; } = null;

        /// <summary>
        /// Dominio de la propiedad (la clase a la que pertenece)
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Rango de la propiedad (el tipo de la propiedad)
        /// </summary>
        public string Range { get; set; }

        /// <summary>
        /// Cardinalidad de la propiedad
        /// </summary>
        public string Cardinality { get; set; }
    }

    public class OwlRelationship
    {
        /// <summary>
        /// Sujeto de la relación (generalmente una clase o propiedad)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Objeto de la relación (generalmente una clase o propiedad relacionada)
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Predicado de la relación (nombre de la propiedad que conecta las entidades)
        /// </summary>
        public string Predicate { get; set; }

        /// <summary>
        /// Tipo de la relación
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OwlRelationshipType Type { get; set; }
    }

    public enum OwlRelationshipType { [EnumMember(Value ="Herencia")] Herencia, [EnumMember(Value ="Asociacion")] Asociacion }
}
