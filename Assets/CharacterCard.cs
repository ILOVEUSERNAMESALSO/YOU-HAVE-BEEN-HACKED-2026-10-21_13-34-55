using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Checkpoint/Character Card")]
public class CharacterCard : ScriptableObject
{
    public string fullName;
    public string seatNumber = "1A";
    public Sprite portrait;
    [TextArea] public string occupationAndPurpose;
    [Tooltip("Dates use dd MMM yyyy, for example 12 MAR 1988.")]
    public string dateOfBirth;
    public string passportName;
    public string passportDOB;
    public string passportExpiry;
    public AudioClip arrivalSound;
    [TextArea(2, 4)] public string[] dialogue;
    [TextArea] public string acceptedResponse;
    [TextArea] public string deniedResponse;
    [TextArea(3, 8)]
    [Tooltip("Optional passenger personality and background for free-form questioning. Keep consistent with this card.")]
    public string roleplayNotes;
    public enum ItemCategory
    {
        Personal,
        Tools
    }

    public enum BaggageLocation
    {
        Cabin,
        Checked
    }

    [Serializable]
    public class CarriedItem
    {
        public string itemName;
        public ItemCategory category;
        public BaggageLocation location;

        [TextArea]
        public string intendedUse;

        public string authorizationReference;
    }

    [Header("Passenger Inventory")]
    public List<CarriedItem> items = new List<CarriedItem>();

    public string DescribeInventory()
    {
        var descriptions = new List<string>();

        if (items != null)
        {
            foreach (CarriedItem item in items)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.itemName))
                    continue;

                string reference =
                    string.IsNullOrWhiteSpace(item.authorizationReference)
                        ? "None supplied"
                        : item.authorizationReference;

                descriptions.Add(
                    $"ITEM: {item.itemName}\n" +
                    $"CATEGORY: {item.category}\n" +
                    $"LOCATION: {item.location} baggage\n" +
                    $"INTENDED USE: {item.intendedUse}\n" +
                    $"AUTHORIZATION REFERENCE: {reference}");
            }
        }

        return descriptions.Count > 0
            ? string.Join("\n\n", descriptions)
            : "Inventory has not been authored. Do not invent belongings.";
    }
    public static bool TryDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(value, "dd MMM yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out date);
    }

    public List<string> FindProblems(DateTime inspectionDate)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(passportName) ||
            !string.Equals(fullName.Trim(), passportName.Trim(), StringComparison.OrdinalIgnoreCase))
            problems.Add("Passport name does not match the registration record.");
        bool realDOBValid = TryDate(dateOfBirth, out var realDOB);
        bool passportDOBValid = TryDate(passportDOB, out var printedDOB);
        if (!realDOBValid || !passportDOBValid || printedDOB > inspectionDate.Date)
            problems.Add("Date of birth is invalid.");
        else if (realDOB != printedDOB)
            problems.Add("Passport DOB does not match the registration record.");
        if (!TryDate(passportExpiry, out var expiry))
            problems.Add("Passport expiry date is invalid.");
        else if (expiry.Date < inspectionDate.Date)
            problems.Add("Passport has expired.");
        if (items != null)
        {
            foreach (CarriedItem item in items)
            {
                if (item == null) continue;

                if (item.category == ItemCategory.Tools &&
                    item.location == BaggageLocation.Cabin)
                {
                    problems.Add(
                        item.itemName +
                        " is in cabin baggage. Tools currently require checking.");
                }
            }
        }
        return problems;
    }
}
