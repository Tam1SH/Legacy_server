


fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_build::compile_protos("./ProtoTypes/logs.proto")?;
    tonic_build::compile_protos("./ProtoTypes/changelogs.proto")?;
    Ok(())
}