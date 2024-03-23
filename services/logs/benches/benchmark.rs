//Не робит. pool timeout.

use std::fs::File;
use pprof::criterion::{PProfProfiler, Output};
use criterion::{criterion_group, criterion_main, Criterion, BenchmarkId};
use tokio::runtime::Runtime;

use logs::test_env::{
	pool::*
};

use logs::{
	repositories::changelogs::{ChangelogsRepository, ChangelogsRepositoryProvider, flush_cache}, 
	pool::Pools
};
use rand::Rng;

async fn benchmark_get_changelogs(pools: &Pools) {    

	let mut cache = pools.redis.get().await.unwrap();
	let db = &pools.db;

	let mut rep = ChangelogsRepository::new(&mut cache, db);
	let mut rng = rand::thread_rng();

	let offset = rng.gen_range(0..88);

	rep.get_changelogs(offset, 12).await.unwrap();

}

async fn flush_cache_(pools : &Pools) {

	let mut cache = pools.redis.get().await.unwrap();
	flush_cache(&mut cache).await;

}
fn criterion_benchmark(c: &mut Criterion) {
	
	let pools = tokio_test::block_on(setup_pools_bench());

	tokio_test::block_on(flush_cache_(&pools));

	let guard = pprof::ProfilerGuardBuilder::default().frequency(1000).blocklist(&["libc", "libgcc", "pthread", "vdso"]).build().unwrap();

    c.bench_with_input(BenchmarkId::new("get_changelogs", "lol"), &pools, |b, pools| {
		
        b.to_async(Runtime::new().unwrap())
		 .iter(|| benchmark_get_changelogs(&pools));

    });

	if let Ok(report) = guard.report().build() {
		let file = File::create("flamegraph.svg").unwrap();
		report.flamegraph(file).unwrap();
	};

}

criterion_group! {
	name = benches;
	config = Criterion::default()
		//.measurement_time(Duration::from_secs(10))
		.with_profiler(PProfProfiler::new(100, Output::Flamegraph(None)));
	targets = criterion_benchmark
}
criterion_main!(benches);
