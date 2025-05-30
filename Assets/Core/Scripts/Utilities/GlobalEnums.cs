
public enum EAuthoritativeMode
{
    Server,
    Owner
}

/* Because we potentially have multiple types to add
 * we either need an extra checkbox in the editor tool or we just add a None here
 * I decide to a None here
 */
public enum NetworkTransformType
{
    None,
    NetworkTransform,
    ClientNetworkTransform,
}
