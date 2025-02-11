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
        // Nombre de la clase
        public string ClassName { get; set; }

        // Etiqueta de la clase, puede ser opcional
        public string Label { get; set; } = null;
    }

    public class OwlProperty
    {
        // Nombre de la propiedad
        public string PropertyName { get; set; }

        // Etiqueta de la propiedad, puede ser opcional
        public string Label { get; set; } = null;

        // Comentario de la propiedad, puede ser opcional
        public string Comment { get; set; } = null;

        // Dominio de la propiedad (la clase a la que pertenece)
        public string Domain { get; set; }

        // Rango de la propiedad (el tipo de la propiedad)
        public string Range { get; set; }

        // Cardinalidad de la propiedad
        public string Cardinality { get; set; }
    }

    public class OwlRelationship
    {
        // Sujeto de la relación (generalmente una clase o propiedad)
        public string Subject { get; set; }

        // Predicado de la relación (nombre de la propiedad que conecta las entidades)
        public string Predicate { get; set; }

        // Objeto de la relación (generalmente una clase o propiedad relacionada)
        public string Object { get; set; }
    }
}
