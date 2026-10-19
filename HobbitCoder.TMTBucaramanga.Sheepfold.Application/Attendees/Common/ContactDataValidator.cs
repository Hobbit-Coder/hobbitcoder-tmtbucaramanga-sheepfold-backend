using FluentValidation;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

/// <summary>Nombre completo, parentesco o relación y teléfono; un contacto incompleto no cuenta.</summary>
public class ContactDataValidator : AbstractValidator<ContactData>
{
    public ContactDataValidator()
    {
        RuleFor(contact => contact.Kind).IsInEnum();
        RuleFor(contact => contact.FullName).NotEmpty().WithMessage("FullName is required").MaximumLength(150);
        RuleFor(contact => contact.Relationship).NotEmpty().WithMessage("Relationship is required").MaximumLength(60);
        RuleFor(contact => contact.PhoneNumber).NotEmpty().WithMessage("PhoneNumber is required").MaximumLength(30);
    }
}
